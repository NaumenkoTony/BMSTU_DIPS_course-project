namespace LoyaltyService.Controllers;

using System.Security.Claims;
using AutoMapper;
using LoyaltyService.Data;
using LoyaltyService.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class LoyaltiesController(ILoyalityRepository repository, IMapper mapper) : Controller
{
    private readonly ILoyalityRepository repository = repository;
    private readonly IMapper mapper = mapper;
    
    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyResponse>> GetByUsername()
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(mapper.Map<LoyaltyResponse>(await repository.GetLoyalityByUsername(username)));
    }

    [Route("/api/v1/[controller]/improve")]
    [HttpGet]
    public async Task<ActionResult> ImproveLoyality()
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await repository.ImproveLoyality(username);
        return Ok();
    }

    
    [Route("/api/v1/[controller]/degrade")]
    [HttpGet]
    public async Task<ActionResult> DegradeLoyality()
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await repository.DegradeLoyality(username);
        return Ok();
    }
}