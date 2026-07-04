using System.Security.Claims;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MfaController(
    IMfaService mfaService,
    IAuthDAL authDAL,
    MediatR.IMediator mediator) : ControllerBase
{
    [Authorize]
    [JwtAuthorization]
    [HttpGet("status")]
    public async Task<IActionResult> GetMfaStatus(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "User identity not found" });
        }

        var mfaSettings = await authDAL.GetMfaSettingsDB(userId, cancellationToken);
        var status = new
        {
            IsConfigured = mfaSettings?.IsMfaConfigured ?? false,
            EmailOtpEnabled = mfaSettings?.EmailOtpEnabled ?? false,
            SmsOtpEnabled = mfaSettings?.SmsOtpEnabled ?? false
        };

        return Ok(new Response<object>
        {
            ReturnCode = StatusCodes.Status200OK,
            ReturnMessage = "MFA status retrieved successfully",
            Data = status
        });
    }

    [Authorize]
    [JwtAuthorization]
    [HttpPost("setup-authenticator")]
    public async Task<IActionResult> SetupAuthenticator(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? "user@domain.com";

        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "User identity not found" });
        }

        var secret = mfaService.GenerateAuthenticatorSecret();
        var qrCodeUri = mfaService.GetQrCodeUri(email, secret);

        var existingMfa = await authDAL.GetMfaSettingsDB(userId, cancellationToken);
        var emailOtp = existingMfa?.EmailOtpEnabled ?? false;
        var smsOtp = existingMfa?.SmsOtpEnabled ?? false;

        // Save secret temporarily (not fully active until verified)
        await authDAL.SaveMfaSettingsDB(userId, emailOtp, smsOtp, secret, false, null, cancellationToken);

        return Ok(new Response<object>
        {
            ReturnCode = StatusCodes.Status200OK,
            ReturnMessage = "Authenticator setup initiated",
            Data = new { Secret = secret, QrCodeUri = qrCodeUri }
        });
    }

    [Authorize]
    [JwtAuthorization]
    [HttpPost("verify-setup")]
    public async Task<IActionResult> VerifySetup([FromBody] MfaCodeRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "User identity not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { Message = "Verification code is required" });
        }

        var mfaSettings = await authDAL.GetMfaSettingsDB(userId, cancellationToken);
        if (mfaSettings is null || string.IsNullOrWhiteSpace(mfaSettings.AuthenticatorSecret))
        {
            return BadRequest(new { Message = "MFA setup was not initiated" });
        }

        var isCodeValid = mfaService.VerifyTotp(mfaSettings.AuthenticatorSecret, request.Code);
        if (!isCodeValid)
        {
            return BadRequest(new Response<object>
            {
                ReturnCode = StatusCodes.Status400BadRequest,
                ReturnMessage = "Invalid code. Setup verification failed."
            });
        }

        var backupCodes = mfaService.GenerateBackupCodes();
        var backupCodesCsv = string.Join(",", backupCodes);

        // Save active settings and backup codes
        await authDAL.SaveMfaSettingsDB(userId, mfaSettings.EmailOtpEnabled, mfaSettings.SmsOtpEnabled, mfaSettings.AuthenticatorSecret, true, backupCodesCsv, cancellationToken);

        return Ok(new Response<object>
        {
            ReturnCode = StatusCodes.Status200OK,
            ReturnMessage = "MFA setup verified and activated successfully.",
            Data = new { BackupCodes = backupCodes }
        });
    }

    [Authorize]
    [JwtAuthorization]
    [HttpPost("disable")]
    public async Task<IActionResult> DisableMfa(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { Message = "User identity not found" });
        }

        await authDAL.SaveMfaSettingsDB(userId, false, false, null, false, null, cancellationToken);

        return Ok(new Response<object>
        {
            ReturnCode = StatusCodes.Status200OK,
            ReturnMessage = "MFA disabled successfully."
        });
    }

    [AllowAnonymous]
    [HttpPost("verify-login")]
    public async Task<IActionResult> VerifyLogin([FromBody] MfaLoginVerifyRequest request, CancellationToken cancellationToken)
    {
        var command = new AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.VerifyMfa.VerifyMfaCommand(
            request.MfaToken,
            request.Code,
            request.RememberMe,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var response = await mediator.Send(command, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    private static int ToHttpStatusCode(int returnCode)
    {
        return returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
    }
}

public sealed class MfaCodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class MfaLoginVerifyRequest
{
    public string MfaToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
