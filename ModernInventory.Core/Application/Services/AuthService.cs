using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModernInventory.Core.Application.DTOs;
using ModernInventory.Core.Domain.Entities;
using ModernInventory.Core.Domain.Enums;
using ModernInventory.Core.Infrastructure.Database;
using ModernInventory.Core.Infrastructure.Security;

namespace ModernInventory.Core.Application.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResultDto> LoginAsync(string email, string password, bool rememberMe = true);
        Task<AuthResultDto> ValidateSessionAsync(string token);
        Task<bool> LogoutAsync(string token);
        Task<bool> ResetPasswordAsync(string email, string securityAnswer, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Email and password are required." };
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser != null)
            {
                return new AuthResultDto { Success = false, ErrorMessage = "An account with this email already exists." };
            }

            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            var business = new BusinessProfile
            {
                Id = Guid.NewGuid().ToString(),
                BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? "My Business" : request.BusinessName.Trim(),
                OwnerName = string.IsNullOrWhiteSpace(request.OwnerName) ? "Owner" : request.OwnerName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim() ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PKR" : request.Currency.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var secQuestions = new Dictionary<string, string>
            {
                { request.SecurityQuestion ?? "What is your primary phone?", request.SecurityAnswer?.Trim().ToLowerInvariant() ?? string.Empty }
            };

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                BusinessId = business.Id,
                Email = normalizedEmail,
                FullName = request.OwnerName.Trim(),
                Phone = request.Phone?.Trim() ?? string.Empty,
                PasswordHash = hash,
                Salt = salt,
                SecurityQuestionsJson = JsonSerializer.Serialize(secQuestions),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Initialize default Pakistani Chart of Accounts
            var defaultAccounts = new List<AccountHead>
            {
                new() { BusinessId = business.Id, Code = "1001", Name = "Cash in Hand", Type = AccountType.ASSET, IsSystem = true, Description = "Cash counter balance" },
                new() { BusinessId = business.Id, Code = "1002", Name = "Bank Account", Type = AccountType.ASSET, IsSystem = true, Description = "Main commercial bank" },
                new() { BusinessId = business.Id, Code = "1003", Name = "Accounts Receivable", Type = AccountType.ASSET, IsSystem = true, Description = "Customer balances" },
                new() { BusinessId = business.Id, Code = "1004", Name = "Inventory", Type = AccountType.ASSET, IsSystem = true, Description = "Merchandise stock valuation" },
                new() { BusinessId = business.Id, Code = "2001", Name = "Accounts Payable", Type = AccountType.LIABILITY, IsSystem = true, Description = "Supplier balances" },
                new() { BusinessId = business.Id, Code = "3001", Name = "Owner Equity / Capital", Type = AccountType.EQUITY, IsSystem = true, Description = "Capital invested" },
                new() { BusinessId = business.Id, Code = "4001", Name = "Sales Revenue", Type = AccountType.REVENUE, IsSystem = true, Description = "Revenue from sales" },
                new() { BusinessId = business.Id, Code = "5001", Name = "Cost of Goods Sold (COGS)", Type = AccountType.EXPENSE, IsSystem = true, Description = "Direct cost of products sold" },
                new() { BusinessId = business.Id, Code = "5002", Name = "Operating Expenses", Type = AccountType.EXPENSE, IsSystem = true, Description = "Utilities, rent, salaries" }
            };

            var session = new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                BusinessId = business.Id,
                Token = Guid.NewGuid().ToString("N"),
                RememberMe = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.MaxValue
            };

            await _context.Businesses.AddAsync(business);
            await _context.Users.AddAsync(user);
            await _context.AccountHeads.AddRangeAsync(defaultAccounts);
            await _context.UserSessions.AddAsync(session);

            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Success = true,
                Token = session.Token,
                UserId = user.Id,
                BusinessId = business.Id,
                FullName = user.FullName,
                BusinessName = business.BusinessName,
                Email = user.Email
            };
        }

        public async Task<AuthResultDto> LoginAsync(string email, string password, bool rememberMe = true)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Email and password are required." };
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Invalid email or password." };
            }

            bool isValid = _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);
            if (!isValid)
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Invalid email or password." };
            }

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == user.BusinessId);

            var session = new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                BusinessId = user.BusinessId,
                Token = Guid.NewGuid().ToString("N"),
                RememberMe = rememberMe,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.MaxValue
            };

            await _context.UserSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Success = true,
                Token = session.Token,
                UserId = user.Id,
                BusinessId = user.BusinessId,
                FullName = user.FullName,
                BusinessName = business?.BusinessName ?? "My Business",
                Email = user.Email
            };
        }

        public async Task<AuthResultDto> ValidateSessionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Invalid token." };
            }

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Token == token && !s.IsRevoked);

            if (session == null)
            {
                return new AuthResultDto { Success = false, ErrorMessage = "Session expired or invalid." };
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == session.UserId);
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == session.BusinessId);

            if (user == null || business == null)
            {
                return new AuthResultDto { Success = false, ErrorMessage = "User or business not found." };
            }

            return new AuthResultDto
            {
                Success = true,
                Token = session.Token,
                UserId = user.Id,
                BusinessId = business.Id,
                FullName = user.FullName,
                BusinessName = business.BusinessName,
                Email = user.Email
            };
        }

        public async Task<bool> LogoutAsync(string token)
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
            if (session != null)
            {
                session.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string securityAnswer, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(securityAnswer) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                return false;
            }

            var questions = JsonSerializer.Deserialize<Dictionary<string, string>>(user.SecurityQuestionsJson ?? "{}");
            if (questions == null || !questions.Values.Any(a => string.Equals(a, securityAnswer.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var (newHash, newSalt) = _passwordHasher.HashPassword(newPassword);
            user.PasswordHash = newHash;
            user.Salt = newSalt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
