namespace Kharasana.Application.Interfaces.Services;

public interface IWhatsAppService
{
    /// <summary>
    /// Sends a WhatsApp template message.
    /// </summary>
    /// <param name="phone">The recipient's phone number in international format (e.g., 967...).</param>
    /// <param name="templateName">The name of the pre-approved WhatsApp template.</param>
    /// <param name="parameters">Parameters to fill in the template placeholders.</param>
    Task<bool> SendTemplateMessageAsync(string phone, string templateName, params string[] parameters);
}
