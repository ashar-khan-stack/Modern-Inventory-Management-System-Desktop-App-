/**
 * Security, Hashing, and Session Validation Engine
 * Implements PBKDF2 with unique cryptographic salt and constant-time comparison.
 * Compatible with C# Rfc2898DeriveBytes (.NET 8 standard).
 */

const PBKDF2_ITERATIONS = 100000;
const KEY_LENGTH_BYTES = 32;

// Generate secure random salt hex string
export function generateSaltHex(byteLength = 16): string {
  const array = new Uint8Array(byteLength);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

// Generate random UUID v4 string
export function generateUUID(): string {
  if (typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

// Convert string to Uint8Array
function stringToBuffer(str: string): Uint8Array {
  return new TextEncoder().encode(str);
}

// Convert hex to Uint8Array
function hexToBuffer(hex: string): Uint8Array {
  const bytes = new Uint8Array(hex.length / 2);
  for (let i = 0; i < bytes.length; i++) {
    bytes[i] = parseInt(hex.substr(i * 2, 2), 16);
  }
  return bytes;
}

// Convert ArrayBuffer to hex
function bufferToHex(buffer: ArrayBuffer): string {
  return Array.from(new Uint8Array(buffer), byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * PBKDF2 Password Hashing
 */
export async function hashPasswordPbkdf2(password: string, saltHex: string): Promise<string> {
  const salt = hexToBuffer(saltHex);
  const passwordBuffer = stringToBuffer(password);

  const keyMaterial = await crypto.subtle.importKey(
    'raw',
    passwordBuffer as unknown as BufferSource,
    { name: 'PBKDF2' },
    false,
    ['deriveBits']
  );

  const derivedBits = await crypto.subtle.deriveBits(
    {
      name: 'PBKDF2',
      salt: salt as unknown as BufferSource,
      iterations: PBKDF2_ITERATIONS,
      hash: 'SHA-256',
    },
    keyMaterial,
    KEY_LENGTH_BYTES * 8
  );

  return bufferToHex(derivedBits);
}

/**
 * Constant-time comparison to prevent timing attacks
 */
export function timingSafeEqual(a: string, b: string): boolean {
  if (a.length !== b.length) {
    return false;
  }
  let result = 0;
  for (let i = 0; i < a.length; i++) {
    result |= a.charCodeAt(i) ^ b.charCodeAt(i);
  }
  return result === 0;
}

/**
 * Verify password against stored hash & salt
 */
export async function verifyPassword(password: string, storedHash: string, saltHex: string): Promise<boolean> {
  const calculatedHash = await hashPasswordPbkdf2(password, saltHex);
  return timingSafeEqual(calculatedHash, storedHash);
}

/**
 * Hash security question answer (case and whitespace normalized)
 */
export async function hashSecurityAnswer(answer: string, saltHex: string): Promise<string> {
  const normalized = answer.trim().toLowerCase();
  return hashPasswordPbkdf2(normalized, saltHex);
}

/**
 * Verify security question answer
 */
export async function verifySecurityAnswer(inputAnswer: string, storedHash: string, saltHex: string): Promise<boolean> {
  const normalized = inputAnswer.trim().toLowerCase();
  const calculatedHash = await hashPasswordPbkdf2(normalized, saltHex);
  return timingSafeEqual(calculatedHash, storedHash);
}

/**
 * Session Expiry Calculation (30 days in milliseconds)
 */
export const THIRTY_DAYS_MS = 30 * 24 * 60 * 60 * 1000;

export function calculateSessionExpiry(rememberMe: boolean): string {
  const now = new Date();
  const duration = rememberMe ? THIRTY_DAYS_MS : 24 * 60 * 60 * 1000; // 30 days if rememberMe, otherwise 24 hrs
  const expiryDate = new Date(now.getTime() + duration);
  return expiryDate.toISOString();
}

export function isSessionExpired(expiresAt: string): boolean {
  const expiry = new Date(expiresAt).getTime();
  const now = Date.now();
  return now >= expiry;
}
