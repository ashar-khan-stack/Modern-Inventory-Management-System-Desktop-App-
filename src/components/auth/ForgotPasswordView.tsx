import React, { useState } from 'react';
import { Mail, Lock, ShieldCheck, ArrowLeft, AlertCircle, CheckCircle2 } from 'lucide-react';
import { AuthService } from '../../services/authService';

interface ForgotPasswordViewProps {
  onBackToLogin: () => void;
}

export const ForgotPasswordView: React.FC<ForgotPasswordViewProps> = ({ onBackToLogin }) => {
  const [step, setStep] = useState<'EMAIL' | 'QUESTIONS' | 'DONE'>('EMAIL');
  const [email, setEmail] = useState('');
  const [questions, setQuestions] = useState<{ question1?: string; question2?: string } | null>(null);
  const [answer1, setAnswer1] = useState('');
  const [answer2, setAnswer2] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleLookupEmail = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');
    const found = AuthService.getSecurityQuestions(email);
    if (!found) {
      setErrorMessage('No business account found for this email or no security questions configured.');
      return;
    }
    setQuestions(found);
    setStep('QUESTIONS');
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    if (newPassword !== confirmPassword) {
      setErrorMessage('New passwords do not match.');
      return;
    }

    if (newPassword.length < 6) {
      setErrorMessage('New password must be at least 6 characters.');
      return;
    }

    setIsSubmitting(true);
    const res = await AuthService.resetPasswordWithSecurityQuestions(
      email,
      { answer1, answer2 },
      newPassword
    );
    setIsSubmitting(false);

    if (res.success) {
      setStep('DONE');
    } else {
      setErrorMessage(res.message);
    }
  };

  return (
    <div className="min-h-screen bg-neutral-900 flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-neutral-950 border border-neutral-800 rounded-xl shadow-2xl p-8 text-neutral-100">
        <div className="flex items-center justify-between pb-4 border-b border-neutral-800 mb-6">
          <h1 className="text-base font-bold text-white">Reset Account Password</h1>
          <button
            id="btn-forgot-back-to-login"
            onClick={onBackToLogin}
            className="flex items-center gap-1 text-xs text-neutral-400 hover:text-white"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Login</span>
          </button>
        </div>

        {errorMessage && (
          <div className="mb-5 p-3 rounded-lg bg-red-950/50 border border-red-800/60 text-red-300 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 text-red-400" />
            <span>{errorMessage}</span>
          </div>
        )}

        {step === 'EMAIL' && (
          <form onSubmit={handleLookupEmail} className="space-y-4">
            <p className="text-xs text-neutral-400">
              Enter your registered business account email to retrieve your salted recovery questions.
            </p>
            <div>
              <label className="block text-xs font-semibold text-neutral-300 mb-1.5">
                Account Email
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500" />
                <input
                  id="forgot-email-input"
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="owner@business.com.pk"
                  className="w-full pl-9 pr-3 py-2 bg-neutral-900 border border-neutral-800 rounded-lg text-xs text-neutral-100 placeholder-neutral-500 focus:outline-none focus:border-emerald-500"
                  required
                />
              </div>
            </div>

            <button
              id="btn-lookup-security-questions"
              type="submit"
              className="w-full py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg shadow-sm transition-colors"
            >
              Continue to Security Questions
            </button>
          </form>
        )}

        {step === 'QUESTIONS' && questions && (
          <form onSubmit={handleResetPassword} className="space-y-4">
            <div className="p-3 bg-neutral-900 border border-neutral-800 rounded-lg space-y-3">
              <div>
                <label className="block text-[11px] text-emerald-400 font-medium mb-1">
                  1. {questions.question1}
                </label>
                <input
                  id="forgot-answer1-input"
                  type="text"
                  value={answer1}
                  onChange={e => setAnswer1(e.target.value)}
                  placeholder="Your answer"
                  className="w-full px-3 py-1.5 bg-neutral-950 border border-neutral-800 rounded text-xs text-neutral-100 focus:outline-none focus:border-emerald-500"
                  required
                />
              </div>

              <div>
                <label className="block text-[11px] text-emerald-400 font-medium mb-1">
                  2. {questions.question2}
                </label>
                <input
                  id="forgot-answer2-input"
                  type="text"
                  value={answer2}
                  onChange={e => setAnswer2(e.target.value)}
                  placeholder="Your answer"
                  className="w-full px-3 py-1.5 bg-neutral-950 border border-neutral-800 rounded text-xs text-neutral-100 focus:outline-none focus:border-emerald-500"
                  required
                />
              </div>
            </div>

            <div className="space-y-3 pt-2">
              <div>
                <label className="block text-xs font-semibold text-neutral-300 mb-1">
                  New Password
                </label>
                <input
                  id="forgot-new-password-input"
                  type="password"
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  placeholder="Minimum 6 characters"
                  className="w-full px-3 py-2 bg-neutral-900 border border-neutral-800 rounded-lg text-xs text-neutral-100 focus:outline-none focus:border-emerald-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-neutral-300 mb-1">
                  Confirm New Password
                </label>
                <input
                  id="forgot-confirm-new-password-input"
                  type="password"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  placeholder="Re-enter new password"
                  className="w-full px-3 py-2 bg-neutral-900 border border-neutral-800 rounded-lg text-xs text-neutral-100 focus:outline-none focus:border-emerald-500"
                  required
                />
              </div>
            </div>

            <button
              id="btn-submit-password-reset"
              type="submit"
              disabled={isSubmitting}
              className="w-full py-2.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white text-xs font-semibold rounded-lg shadow-sm transition-colors"
            >
              {isSubmitting ? 'Verifying & Updating...' : 'Reset Password & Update Local DB'}
            </button>
          </form>
        )}

        {step === 'DONE' && (
          <div className="text-center py-4 space-y-4">
            <div className="w-12 h-12 rounded-full bg-emerald-950 border border-emerald-700 text-emerald-400 flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">Password Reset Successful</h3>
              <p className="text-xs text-neutral-400 mt-1">
                Your credentials have been securely updated with new PBKDF2 hashing.
              </p>
            </div>
            <button
              id="btn-reset-success-login"
              onClick={onBackToLogin}
              className="w-full py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg"
            >
              Return to Login
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
