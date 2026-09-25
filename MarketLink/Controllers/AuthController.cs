using MarketLink.DTOs;
using MarketLink.Models;
using MarketLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthController(
            ApplicationDbContext context,
            PasswordService passwordService,
            TokenService tokenService,
            IConfiguration configuration)
                {
                    _context = context;
                    _passwordService = passwordService;
                    _tokenService = tokenService;
                    _configuration = configuration;
                }

        [HttpPost("register/customer")]
        public async Task<IActionResult> RegisterCustomer(
            RegisterCustomerDto dto)
        {
            // Check if email already exists
            bool emailExists = await _context.users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "An account with this email already exists."
                });
            }

            // Find Customer role
            var role = await _context.roles
                .FirstOrDefaultAsync(r =>
                    r.RoleName.ToLower() == "customer");

            if (role == null)
            {
                return BadRequest(new
                {
                    message = "Customer role does not exist."
                });
            }

            // Create User
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                RoleId = role.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.users.Add(user);

            await _context.SaveChangesAsync();

            // Create Customer profile
            var customer = new Customer
            {
                UserId = user.UserId,
                CreatedAt = DateTime.Now
            };

            _context.customers.Add(customer);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Customer registered successfully.",
                userId = user.UserId,
                customerId = customer.CustomerId
            });
        }


        [HttpPost("register/farmer")]
        public async Task<IActionResult> RegisterFarmer(
            RegisterFarmerDto dto)
        {
            // Check if email already exists
            bool emailExists = await _context.users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "An account with this email already exists."
                });
            }

            // Find Farmer role
            var role = await _context.roles
                .FirstOrDefaultAsync(r =>
                    r.RoleName.ToLower() == "farmer");

            if (role == null)
            {
                return BadRequest(new
                {
                    message = "Farmer role does not exist."
                });
            }

            // Create User
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                RoleId = role.RoleId,
                IsActive = false,   
                CreatedAt = DateTime.Now
            };

            _context.users.Add(user);

            await _context.SaveChangesAsync();

            // Create Farmer profile
            var farmer = new Farmer
            {
                UserId = user.UserId,
                BusinessName = dto.BusinessName,
                Description = dto.Description,
                Address = dto.Address,
                IsApproved = false,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.farmers.Add(farmer);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Farmer registered successfully. Waiting for admin approval.",
                userId = user.UserId,
                farmerId = farmer.FarmerId
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // Find user by email
            var user = await _context.users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Check if account is active
            if (!user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Your account is inactive."
                });
            }

            // Verify password
            bool passwordValid = _passwordService.VerifyPassword(
                dto.Password,
                user.PasswordHash
            );

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Update last login
            user.LastLoginAt = DateTime.Now;

            // Generate access token
            string accessToken = _tokenService.CreateAccessToken(user);

            Response.Cookies.Append(
                "accessToken",
                accessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(
                        int.Parse(
                            _configuration["Jwt:ExpiryMinutes"] ?? "30"
                        )
                    )
                }
            );

            // Generate refresh token
            string refreshToken = _tokenService.CreateRefreshToken();

            var refreshTokenDays = int.Parse(
                _configuration["Jwt:RefreshTokenDays"] ?? "7"
            );

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
                CreatedAt = DateTime.Now,
                IsRevoked = false
            };

            _context.refreshtokens.Add(refreshTokenEntity);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Login successful.",
                accessToken = accessToken,
                refreshToken = refreshToken,
                expiresInMinutes = int.Parse(
                    _configuration["Jwt:ExpiryMinutes"] ?? "30"
                ),
                user = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    role = user.Role?.RoleName
                }
            });
        }







        [HttpGet("/Auth/Register")]
        public IActionResult Register()
        {
            return View();
        }


        [HttpGet("/Auth/Login")]
        public IActionResult Login()
        {
            return View();
        }


    }
}