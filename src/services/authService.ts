/**
 * Authentication and User Session Management Service
 * Implements PBKDF2 with unique salts, 30-day session management,
 * security question verification, and multi-tenant isolation.
 */

import { BusinessProfile, SecurityQuestion, User, UserSession } from '../types';
import {
  calculateSessionExpiry,
  generateSaltHex,
  generateUUID,
  hashPasswordPbkdf2,
  hashSecurityAnswer,
  isSessionExpired,
  verifyPassword,
  verifySecurityAnswer,
} from '../domain/security';
import { localDb } from '../infrastructure/storage/localDatabase';
import { DEFAULT_CURRENCY } from '../domain/constants';

const ACTIVE_SESSION_STORAGE_KEY = 'modern_inv_active_session_token';

export interface RegisterBusinessDto {
  businessName: string;
  ownerName: string;
  email: string;
  phone: string;
  address: string;
  ntnNumber: string;
  strnNumber: string;
  password: string;
  securityQuestions: {
    question: string;
    answer: string;
  }[];
}

export class AuthService {
  /**
   * Register a new Business Profile and Owner Account
   */
  public static async registerBusiness(dto: RegisterBusinessDto): Promise<{ success: boolean; message: string; session?: UserSession }> {
    const existingUser = localDb.getUserByEmail(dto.email);
    if (existingUser) {
      return { success: false, message: 'An account with this email address already exists.' };
    }

    if (dto.password.length < 6) {
      return { success: false, message: 'Password must be at least 6 characters long.' };
    }

    if (!dto.securityQuestions || dto.securityQuestions.length < 2) {
      return { success: false, message: 'Please provide at least 2 security questions for account recovery.' };
    }

    const now = new Date().toISOString();
    const businessId = generateUUID();
    const userId = generateUUID();

    // Create Business Profile
    const business: BusinessProfile = {
      id: businessId,
      businessName: dto.businessName.trim(),
      ownerName: dto.ownerName.trim(),
      email: dto.email.trim().toLowerCase(),
      phone: dto.phone.trim(),
      address: dto.address.trim(),
      ntnNumber: dto.ntnNumber.trim(),
      strnNumber: dto.strnNumber.trim(),
      currency: DEFAULT_CURRENCY,
      createdAt: now,
      updatedAt: now,
    };

    localDb.insertBusiness(business);

    // Hash Password with PBKDF2
    const salt = generateSaltHex(16);
    const passwordHash = await hashPasswordPbkdf2(dto.password, salt);

    // Hash Security Questions
    const processedQuestions: SecurityQuestion[] = [];
    for (const q of dto.securityQuestions) {
      const qSalt = generateSaltHex(16);
      const answerHash = await hashSecurityAnswer(q.answer, qSalt);
      processedQuestions.push({
        question: q.question,
        answerHash,
        salt: qSalt,
      });
    }

    // Create User
    const user: User = {
      id: userId,
      businessId,
      email: dto.email.trim().toLowerCase(),
      fullName: dto.ownerName.trim(),
      phone: dto.phone.trim(),
      passwordHash,
      salt,
      securityQuestions: processedQuestions,
      createdAt: now,
      updatedAt: now,
    };

    localDb.insertUser(user);

    // Seed default Chart of Accounts for the new business
    localDb.seedDefaultAccountHeads(businessId);

    // Create initial 30-day session
    const session = await this.createSession(user, business, true);

    return { success: true, message: 'Business account created successfully!', session };
  }

  /**
   * Authenticate user with Email & Password
   */
  public static async login(email: string, password: string, rememberMe = true): Promise<{ success: boolean; message: string; session?: UserSession }> {
    const user = localDb.getUserByEmail(email);
    if (!user) {
      return { success: false, message: 'Invalid email or password.' };
    }

    const isMatch = await verifyPassword(password, user.passwordHash, user.salt);
    if (!isMatch) {
      return { success: false, message: 'Invalid email or password.' };
    }

    const business = localDb.getBusiness(user.businessId);
    if (!business) {
      return { success: false, message: 'Associated business account not found.' };
    }

    const session = await this.createSession(user, business, rememberMe);
    return { success: true, message: 'Login successful.', session };
  }

  /**
   * Creates and stores a 30-day session
   */
  private static async createSession(user: User, business: BusinessProfile, rememberMe: boolean): Promise<UserSession> {
    const now = new Date().toISOString();
    const token = generateUUID();
    const session: UserSession = {
      sessionId: generateUUID(),
      userId: user.id,
      businessId: business.id,
      email: user.email,
      businessName: business.businessName,
      token,
      createdAt: now,
      expiresAt: calculateSessionExpiry(rememberMe),
      rememberMe,
    };

    localDb.insertSession(session);
    localStorage.setItem(ACTIVE_SESSION_STORAGE_KEY, token);
    return session;
  }

  /**
   * Validates active session on startup or protected action
   */
  public static validateCurrentSession(): UserSession | null {
    const token = localStorage.getItem(ACTIVE_SESSION_STORAGE_KEY);
    if (!token) return null;

    const session = localDb.getActiveSession(token);
    if (!session) {
      localStorage.removeItem(ACTIVE_SESSION_STORAGE_KEY);
      return null;
    }

    if (isSessionExpired(session.expiresAt)) {
      // Expired 30-day session -> clear and force login
      localDb.deleteSession(token);
      localStorage.removeItem(ACTIVE_SESSION_STORAGE_KEY);
      return null;
    }

    return session;
  }

  /**
   * Log out active session
   */
  public static logout(): void {
    const token = localStorage.getItem(ACTIVE_SESSION_STORAGE_KEY);
    if (token) {
      localDb.deleteSession(token);
      localStorage.removeItem(ACTIVE_SESSION_STORAGE_KEY);
    }
  }

  /**
   * Retrieve security questions for password recovery
   */
  public static getSecurityQuestions(email: string): { question1?: string; question2?: string } | null {
    const user = localDb.getUserByEmail(email);
    if (!user || !user.securityQuestions || user.securityQuestions.length < 2) {
      return null;
    }
    return {
      question1: user.securityQuestions[0].question,
      question2: user.securityQuestions[1].question,
    };
  }

  /**
   * Verify recovery answers and reset password
   */
  public static async resetPasswordWithSecurityQuestions(
    email: string,
    answers: { answer1: string; answer2: string },
    newPassword: string
  ): Promise<{ success: boolean; message: string }> {
    const user = localDb.getUserByEmail(email);
    if (!user || !user.securityQuestions || user.securityQuestions.length < 2) {
      return { success: false, message: 'User not found or security questions not configured.' };
    }

    if (newPassword.length < 6) {
      return { success: false, message: 'New password must be at least 6 characters long.' };
    }

    const q1 = user.securityQuestions[0];
    const q2 = user.securityQuestions[1];

    const isMatch1 = await verifySecurityAnswer(answers.answer1, q1.answerHash, q1.salt);
    const isMatch2 = await verifySecurityAnswer(answers.answer2, q2.answerHash, q2.salt);

    if (!isMatch1 || !isMatch2) {
      return { success: false, message: 'One or more security question answers are incorrect.' };
    }

    // Hash new password with fresh salt
    const newSalt = generateSaltHex(16);
    const newPasswordHash = await hashPasswordPbkdf2(newPassword, newSalt);

    user.passwordHash = newPasswordHash;
    user.salt = newSalt;
    localDb.updateUser(user);

    return { success: true, message: 'Password has been successfully reset! You can now log in.' };
  }
}
