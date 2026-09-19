/**
 * Current User & Business Tenant Context Provider
 * Guarantees that every repository query and UI view is strictly scoped to the active BusinessId.
 */

import React, { createContext, useContext, useEffect, useState } from 'react';
import { BusinessProfile, UserSession } from '../../types';
import { AuthService } from '../../services/authService';
import { localDb } from '../storage/localDatabase';

interface CurrentUserContextType {
  session: UserSession | null;
  business: BusinessProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string, rememberMe: boolean) => Promise<{ success: boolean; message: string }>;
  logout: () => void;
  refreshSession: () => void;
}

const CurrentUserContext = createContext<CurrentUserContextType>({
  session: null,
  business: null,
  isAuthenticated: false,
  isLoading: true,
  login: async () => ({ success: false, message: '' }),
  logout: () => {},
  refreshSession: () => {},
});

export const CurrentUserProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [session, setSession] = useState<UserSession | null>(null);
  const [business, setBusiness] = useState<BusinessProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const checkSession = () => {
    const validSession = AuthService.validateCurrentSession();
    if (validSession) {
      setSession(validSession);
      const b = localDb.getBusiness(validSession.businessId);
      setBusiness(b || null);
    } else {
      setSession(null);
      setBusiness(null);
    }
    setIsLoading(false);
  };

  useEffect(() => {
    checkSession();
    const unsubscribe = localDb.subscribe(() => {
      // If business updated in DB, sync local context state
      if (session) {
        const b = localDb.getBusiness(session.businessId);
        setBusiness(b || null);
      }
    });
    return () => unsubscribe();
  }, []);

  const handleLogin = async (email: string, password: string, rememberMe: boolean) => {
    const res = await AuthService.login(email, password, rememberMe);
    if (res.success && res.session) {
      setSession(res.session);
      const b = localDb.getBusiness(res.session.businessId);
      setBusiness(b || null);
    }
    return { success: res.success, message: res.message };
  };

  const handleLogout = () => {
    AuthService.logout();
    setSession(null);
    setBusiness(null);
  };

  return (
    <CurrentUserContext.Provider
      value={{
        session,
        business,
        isAuthenticated: !!session,
        isLoading,
        login: handleLogin,
        logout: handleLogout,
        refreshSession: checkSession,
      }}
    >
      {children}
    </CurrentUserContext.Provider>
  );
};

export const useCurrentUser = () => useContext(CurrentUserContext);
