﻿using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using PatikaFinalProject.Common;
using PatikaFinalProject.DataAccess;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PatikaFinalProject.Bussiness.Services
{
    public class RegisterLoginService
    {
        private readonly MyDbContext dbContext;
        private readonly IValidator<LoginRequestModel> loginRequestValidator;
        private readonly IValidator<RegistrationRequestModel> registrationRequestValidator;
        
        public RegisterLoginService(MyDbContext dbContext, IMapper mapper, IValidator<LoginRequestModel> loginRequestValidator, IValidator<RegistrationRequestModel> registrationRequestValidator)
        {
            this.dbContext = dbContext;
            this.loginRequestValidator = loginRequestValidator;
            this.registrationRequestValidator = registrationRequestValidator;
        }

        public IResponse<RegistrationRequestModel> ValidateCredentials(LoginRequestModel userModel)
        {
            loginRequestValidator.Validate(userModel);

            byte[] hashedPass = System.Security.Cryptography.SHA512.HashData(Encoding.UTF8.GetBytes(userModel.Password + userModel.UserName));

            User? user = dbContext.Set<User>().SingleOrDefault(x => x.UserName == userModel.UserName && x.HashedPass == hashedPass);

            if (user == null)
            {
                return new Response<RegistrationRequestModel>(ResponseType.NotFound, "Not Found");
            }
            return new Response<RegistrationRequestModel>(ResponseType.Success, new RegistrationRequestModel(user.UserName, userModel.Password, user.UserType));
        }

        public LoginResponseModel GenerateToken(RegistrationRequestModel userModel)
        {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("How much is this static key secure ?"));

            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            List<Claim> claims = new List<Claim>() { new Claim(ClaimTypes.Role, userModel.UserType) };

            JwtSecurityToken token = new JwtSecurityToken(issuer: "http://localhost", claims: claims, audience: "http://localhost", notBefore: DateTime.Now, expires: DateTime.Now.AddMinutes(1000), signingCredentials: credentials);
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            return new LoginResponseModel(handler.WriteToken(token), userModel.UserName);
        }

        public async Task<IResponse> Register(RegistrationRequestModel userModel)
        {
            registrationRequestValidator.Validate(userModel);

            IResponse response;
            User? user = dbContext.Set<User>().SingleOrDefault(x => x.UserName == userModel.UserName);
            if(user == null)
            {
                byte[] hashedPass = System.Security.Cryptography.SHA512.HashData(Encoding.UTF8.GetBytes(userModel.Password+userModel.UserName));

                await dbContext.Set<User>().AddAsync(new User(userModel.UserName, hashedPass, userModel.UserType));
                await dbContext.SaveChangesAsync();

                response = new Response(ResponseType.Success, "Successfully registered.");
            }
            else
            {
                response = new Response(ResponseType.ValidationError, "Username already exist.");
            }
            return response;
        }
    }

    public class LoginRequestModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponseModel
    {
        public string UserName { get; set; }
        public string Token { get; set; }

        public LoginResponseModel(string token, string userName)
        {
            Token = token;
            UserName = userName;
        }
    }

    public class RegistrationRequestModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserType { get; set; } // Member, Admin

        public RegistrationRequestModel() { }
        public RegistrationRequestModel(string userName, string userPassword, string userType)
        {
            UserName = userName;
            UserType = userType;
            Password = userPassword;
        }
    }
}
