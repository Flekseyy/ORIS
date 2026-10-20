using System.Net;
using MyHttpServer.Framework.Result;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace MyHttpServer.Framework.Services;

public static class EmailService
{
    private const string group = "11-507";
    private const string fio = "Смолянинов Алексей Викторович";
    public static async Task SendEmailAsync(string to)
    {

        var message = new MimeMessage();
    
        message.From.Add(new MailboxAddress("Алексей","testmmailtest@mail.ru" ));
    
        message.To.Add(MailboxAddress.Parse(to));

        message.Subject = $"Домашняя работа от {fio} ";
    
        var builder = new BodyBuilder
        {
            TextBody = $"Homework от {fio} из группы {group}" 
        };
        var staticDir = Path.Combine(Directory.GetCurrentDirectory(), "static");
        var localPath = Path.Combine(staticDir, "Homework.zip");
    
        if (File.Exists(localPath))
        {
            await builder.Attachments.AddAsync(localPath);
        }
    
        message.Body = builder.ToMessageBody();
    
   
        using var client = new SmtpClient();
        
        await client.ConnectAsync("smtp.mail.ru", 465 , SecureSocketOptions.SslOnConnect);
    

        await client.AuthenticateAsync("testmmailtest@mail.ru", "....");

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    
   
    }
}

