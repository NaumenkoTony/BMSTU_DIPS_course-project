namespace LoyaltyService.Controllers;

using AutoMapper;
using Contracts.Dto;
using LoyaltyService.Data;
using LoyaltyService.TokenService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class LoyaltiesController(ILoyalityRepository repository, IMapper mapper, ITokenService tokenService) : Controller
{
    private readonly ILoyalityRepository repository = repository;
    private readonly IMapper mapper = mapper;
    private readonly ITokenService tokenService = tokenService;

    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyResponse>> GetByUsername()
    {
        string username = tokenService.GetUsernameFromJWT();
        return Ok(mapper.Map<LoyaltyResponse>(await repository.GetLoyalityByUsername(username)));
    }

    [Route("/api/v1/[controller]/improve")]
    [HttpGet]
    public async Task<ActionResult> ImproveLoyality()
    {
        string username = tokenService.GetUsernameFromJWT();
        await repository.ImproveLoyality(username);
        return Ok();
    }


    [Route("/api/v1/[controller]/degrade")]
    [HttpGet]
    public async Task<ActionResult> DegradeLoyality()
    {
        string username = tokenService.GetUsernameFromJWT();
        await repository.DegradeLoyality(username);
        return Ok();
    }

    [Route("/api/v1/[controller]/create-user")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> CreateLoyalityUser([FromBody] string username)
    {
        await repository.CreateLoyalityUser(username);
        return Ok();
    }
}