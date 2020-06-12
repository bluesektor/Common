// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

//using Logging;
namespace Omni.Managers.Services
{
    using Newtonsoft.Json;
    using System;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using GreenWerx.Data.Logging;
    using GreenWerx.Models.App;
    using GreenWerx.Models.Services;
    using GreenWerx.Utilites.Extensions;
    using GreenWerx.Utilites.Security;

    public class SMTP
    {
        private readonly SystemLogger _logger = null;

        private readonly EmailSettings _settings = new EmailSettings();

        private readonly string className = MethodBase.GetCurrentMethod().DeclaringType.ToString();

        public SMTP(string connectionKey, EmailSettings settings)
        {
            HasError = false;
            ErrorMessage = "";

            _logger = new SystemLogger(connectionKey);
            _settings.MailPort = 587;
            _settings.MailHost = "smtp.gmail.com";

            _settings = settings;

            if (string.IsNullOrWhiteSpace(settings.MailHost))
            {
                SetStatus(true, "The applications email host is not set in the config file.", false);
                _logger.InsertError(this.ErrorMessage, "SMTP", "constructor");
            }

            if (settings.MailPort <= 0)
            {
                SetStatus(true, "The applications email host port is not set in the config file.", true);
                _logger.InsertError(this.ErrorMessage, "SMTP", "constructor");
            }

            if (string.IsNullOrWhiteSpace(settings.HostUser))
            {
                SetStatus(true, "The applications email host user is not set in the config file.", true);
                _logger.InsertError(this.ErrorMessage, "SMTP", "constructor");
            }

            if (string.IsNullOrWhiteSpace(settings.HostPassword))
            {
                SetStatus(true, "The applications email host password is not set in the config file.", true);
                _logger.InsertError(this.ErrorMessage, "SMTP", "constructor");
            }
        }

        public string ErrorMessage { get; set; }
        public bool HasError { get; set; }
        public string SiteFromAddress { get; set; }

        public ServiceResult SendMail(MailMessage msg)
        {
            if (HasError)
                return ServiceResponse.Error("Error sending email.");

            try
            {
                if (string.IsNullOrWhiteSpace(_settings.EncryptionKey))
                    return ServiceResponse.Error("The encryption key is not set.");

                string hostPassword = Cipher.Crypt(_settings.EncryptionKey, _settings.HostPassword, false);

                //to user gmail without oauth you need to turn on 2 step verification in gmail account and generate an app key.
                SmtpClient smtp = new SmtpClient();

                smtp.Host = _settings.MailHost;
                smtp.Port = _settings.MailPort;
                smtp.EnableSsl = _settings.UseSSL;
                //if (smtp.Host.Contains("gmail"))//this wasn't needed for other hosts.
                // {
                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                // }
                //   smtp.Timeout = 20000
                smtp.Credentials = new NetworkCredential(_settings.HostUser, hostPassword);

                try
                {
                    _logger.InsertInfo(JsonConvert.SerializeObject(msg), "SMTP", "SendMail(MailMessage)");
                }
                catch { }
                Disable_CertificateValidation();//shouldn't be used in prod, but tmdhosting is throwing bad cert.
                smtp.Send(msg);
            }
            catch (SmtpException smtpex)
            {
                _logger.InsertError(smtpex.DeserializeException(true), "SMTP", "SendMail(MailMessage)");
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.DeserializeException(true), "SMTP", "SendMail(MailMessage)");
                return ServiceResponse.Error("Failed to send email.");
            }

            return ServiceResponse.OK();
        }

        public bool testsend(EmailSettings settings)
        {
            try
            {
                SmtpClient smtp = new SmtpClient(settings.MailHost);
                smtp.Host = settings.MailHost;
                smtp.Port = settings.MailPort; //  8889
                smtp.EnableSsl = _settings.UseSSL;

                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                //IMPORANT:  Your smtp login email MUST be same as your FROM address.
                if (string.IsNullOrWhiteSpace(_settings.EncryptionKey))
                    return false;

                // string hostPassword = Cipher.Crypt(_settings.EncryptionKey, _settings.HostPassword, false);
                string hostPassword = settings.HostPassword;

                NetworkCredential Credentials = new NetworkCredential(settings.HostUser, hostPassword);
                smtp.Credentials = Credentials;

                MailMessage mail = new MailMessage();

                //set the addresses
                //IMPORTANT: This must be same as your smtp authentication address.
                mail.From = new MailAddress(settings.HostUser, settings.EmailDomain); //"USERNAME@DOMAIN.com");
                mail.To.Add("stephen.osterhoudt@gmail.com");

                //set the content
                mail.Subject = "This is an email";
                mail.Body = JsonConvert.SerializeObject(settings);
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                string Message = ex.Message;
                if (ex.InnerException != null)
                    Message += ex.InnerException;

                _logger.InsertError(Message, "SMTP", MethodInfo.GetCurrentMethod().Name);
                return false;
            }
            _logger.InsertInfo("sent email", "smtp", "testsend");
            return true;
        }

        protected void SetStatus(bool hasError, string message, bool appendMessage)
        {
            HasError = hasError;
            if (appendMessage)
                ErrorMessage += message;
            else
                ErrorMessage = message;
        }

        // public async Task<ServiceResult> SendMailAsync(MailMessage msg)
        //public async Task<ServiceResult> SendMailAsync(MailMessage msg)
        //{
        //    if (HasError)
        //    {
        //        _logger.InsertError(ErrorMessage, "smtp.cs", "SendMailAsync");
        //        return ServiceResponse.Error(ErrorMessage);
        //    }

        //    try
        //    {
        //        if(string.IsNullOrWhiteSpace(_settings.EncryptionKey))
        //            return ServiceResponse.Error("The encryption key is not set.");

        //        //Open buildall project and use test functions to enc/dec password
        //        //Api_ToolController_Cipher_Encrypt
        //        //Api_ToolController_Cipher_Decrypt
        //      // var emailenc  = Cipher.Crypt(_settings.EncryptionKey, "@Th3Dug#P4rty", true);

        //         //to use gmail without oauth you need to turn on 2 step verification in gmail account and generate an app key.

        //        SmtpClient smtp = new SmtpClient();
        //        smtp.Host = _settings.MailHost;
        //        smtp.Port = _settings.MailPort;
        //        smtp.EnableSsl =   _settings.UseSSL;
        //        if (smtp.Host.Contains("gmail"))//this wasn't needed for other hosts.
        //            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        //        //   smtp.Timeout = 20000

        //        smtp.Send(msg);
        //        _logger.InsertInfo(JsonConvert.SerializeObject(msg), "smtp.cs", "sendmailasync");
        //        // await smtp.SendMailAsync(msg);

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage = ex.DeserializeException(true);
        //        _logger.InsertError(ErrorMessage, "SMTP", "SendMail()");
        //        _logger.InsertError(JsonConvert.SerializeObject(msg), "SMTP", "SendMail().Message:");
        //        return ServiceResponse.Error("Error sending email.");
        //    }

        //   return ServiceResponse.OK();
        //}
        private static void Disable_CertificateValidation()
        {
            // Disabling certificate validation can expose you to a man-in-the-middle attack
            // which may allow your encrypted message to be read by an attacker
            // https://stackoverflow.com/a/14907718/740639
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (
                    object s,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors
                )
                {
                    return true;
                };
        }
    }
}