import React, { useState } from 'react';
import { CurrentUserProvider, useCurrentUser } from './infrastructure/context/CurrentUserContext';
import { DesktopWindowFrame } from './components/layout/DesktopWindowFrame';
import { TopAppBar } from './components/layout/TopAppBar';
import { Sidebar } from './components/layout/Sidebar';
import { LoginView } from './components/auth/LoginView';
import { RegisterView } from './components/auth/RegisterView';
import { ForgotPasswordView } from './components/auth/ForgotPasswordView';
import { DashboardShellView } from './components/dashboard/DashboardShellView';
import { BackupSyncShellView } from './components/settings/BackupSyncShellView';
import { ShellPlaceholderView } from './components/common/ShellPlaceholderView';
import { Phase1TestRunnerModal } from './components/testing/Phase1TestRunnerModal';
import { Loader2 } from 'lucide-react';

enum AuthScreenState {
  LOGIN = 'LOGIN',
  REGISTER = 'REGISTER',
  FORGOT_PASSWORD = 'FORGOT_PASSWORD',
}

const DesktopAppContent: React.FC = () => {
  const { isAuthenticated, isLoading, business } = useCurrentUser();
  const [authScreen, setAuthScreen] = useState<AuthScreenState>(AuthScreenState.LOGIN);
  const [currentModule, setCurrentModule] = useState('dashboard');
  const [darkMode, setDarkMode] = useState(true);
  const [isTestRunnerOpen, setIsTestRunnerOpen] = useState(false);

  // Loading Splash
  if (isLoading) {
    return (
      <div className="min-h-screen bg-neutral-900 flex flex-col items-center justify-center text-neutral-100">
        <div className="w-12 h-12 rounded-xl bg-emerald-600 flex items-center justify-center font-bold text-xl shadow-lg mb-4">
          M
        </div>
        <div className="flex items-center gap-2 text-xs text-neutral-400">
          <Loader2 className="w-4 h-4 animate-spin text-emerald-500" />
          <span>Verifying Persistent Session &amp; SQLite Storage...</span>
        </div>
      </div>
    );
  }

  // Not Logged In -> Show Auth Screens
  if (!isAuthenticated) {
    if (authScreen === AuthScreenState.REGISTER) {
      return <RegisterView onGoToLogin={() => setAuthScreen(AuthScreenState.LOGIN)} />;
    }
    if (authScreen === AuthScreenState.FORGOT_PASSWORD) {
      return <ForgotPasswordView onBackToLogin={() => setAuthScreen(AuthScreenState.LOGIN)} />;
    }
    return (
      <LoginView
        onGoToRegister={() => setAuthScreen(AuthScreenState.REGISTER)}
        onGoToForgotPassword={() => setAuthScreen(AuthScreenState.FORGOT_PASSWORD)}
      />
    );
  }

  // Logged In -> Render Main Windows Desktop Shell
  return (
    <div className={`h-screen w-screen flex flex-col overflow-hidden font-sans ${darkMode ? 'dark bg-neutral-950 text-neutral-100' : 'bg-neutral-100 text-neutral-900'}`}>
      {/* Windows Title Bar Frame */}
      <DesktopWindowFrame businessName={business?.businessName} />

      {/* Top Application Bar */}
      <TopAppBar
        darkMode={darkMode}
        onToggleDarkMode={() => setDarkMode(!darkMode)}
        onOpenTestRunner={() => setIsTestRunnerOpen(true)}
        onNavigate={module => setCurrentModule(module)}
      />

      {/* Main Content Area (Sidebar + Active Screen) */}
      <div className="flex-1 flex overflow-hidden">
        {/* Navigation Sidebar */}
        <Sidebar
          currentModule={currentModule}
          onSelectModule={mod => setCurrentModule(mod)}
          darkMode={darkMode}
        />

        {/* Dynamic Viewport */}
        <main className={`flex-1 overflow-y-auto ${darkMode ? 'bg-neutral-950' : 'bg-neutral-100/70'}`}>
          {currentModule === 'dashboard' && (
            <DashboardShellView
              darkMode={darkMode}
              onNavigate={mod => setCurrentModule(mod)}
            />
          )}

          {currentModule === 'settings-backup' && (
            <BackupSyncShellView darkMode={darkMode} />
          )}

          {currentModule !== 'dashboard' && currentModule !== 'settings-backup' && (
            <ShellPlaceholderView module={currentModule} darkMode={darkMode} />
          )}
        </main>
      </div>

      {/* Phase 1 Verification Test Suite Modal */}
      <Phase1TestRunnerModal
        isOpen={isTestRunnerOpen}
        onClose={() => setIsTestRunnerOpen(false)}
        darkMode={darkMode}
      />
    </div>
  );
};

export default function App() {
  return (
    <CurrentUserProvider>
      <DesktopAppContent />
    </CurrentUserProvider>
  );
}
