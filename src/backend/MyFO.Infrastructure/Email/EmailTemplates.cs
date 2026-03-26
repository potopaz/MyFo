namespace MyFO.Infrastructure.Email;

public static class EmailTemplates
{
    // MyFO brand colors
    private const string PrimaryBlue = "#3B82F6";
    private const string DarkBlue = "#2563EB";

    private static string Layout(string content)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 0; color: #1a1a1a; background-color: #f8fafc;">
            <div style="background-color: {DarkBlue}; padding: 24px; text-align: center;">
                <h1 style="font-size: 28px; font-weight: 700; color: #ffffff; margin: 0; letter-spacing: -0.5px;">MyFO</h1>
            </div>
            <div style="background-color: #ffffff; padding: 32px 24px; border-left: 1px solid #e5e7eb; border-right: 1px solid #e5e7eb;">
                {content}
            </div>
            <div style="background-color: #f1f5f9; padding: 16px 24px; text-align: center; border-top: 1px solid #e5e7eb;">
                <p style="color: #94a3b8; font-size: 12px; margin: 0;">MyFO — Organizá tus finanzas familiares</p>
            </div>
        </body>
        </html>
        """;
    }

    public static string VerifyEmail(string fullName, string verifyUrl)
    {
        var content = $"""
                <h2 style="font-size: 20px; margin: 0 0 16px; color: #0f172a;">Verificá tu email</h2>
                <p style="margin: 0 0 12px; line-height: 1.6;">Hola <strong>{fullName}</strong>,</p>
                <p style="margin: 0 0 24px; line-height: 1.6;">Gracias por registrarte en MyFO. Hacé click en el botón para verificar tu email y activar tu cuenta:</p>
                <div style="text-align: center; margin: 32px 0;">
                    <a href="{verifyUrl}" style="background-color: {PrimaryBlue}; color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; display: inline-block;">Verificar email</a>
                </div>
                <p style="color: #64748b; font-size: 14px; margin: 0 0 8px;">Este link expira en 24 horas.</p>
                <p style="color: #64748b; font-size: 14px; margin: 0;">Si no creaste esta cuenta, podés ignorar este email.</p>
        """;
        return Layout(content);
    }

    public static string ResetPassword(string fullName, string resetUrl)
    {
        var content = $"""
                <h2 style="font-size: 20px; margin: 0 0 16px; color: #0f172a;">Recuperar contraseña</h2>
                <p style="margin: 0 0 12px; line-height: 1.6;">Hola <strong>{fullName}</strong>,</p>
                <p style="margin: 0 0 24px; line-height: 1.6;">Recibimos una solicitud para restablecer tu contraseña. Hacé click en el botón para crear una nueva:</p>
                <div style="text-align: center; margin: 32px 0;">
                    <a href="{resetUrl}" style="background-color: {PrimaryBlue}; color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; display: inline-block;">Restablecer contraseña</a>
                </div>
                <p style="color: #64748b; font-size: 14px; margin: 0 0 8px;">Este link expira en 1 hora.</p>
                <p style="color: #64748b; font-size: 14px; margin: 0;">Si no solicitaste esto, podés ignorar este email. Tu contraseña no cambiará.</p>
        """;
        return Layout(content);
    }
}
