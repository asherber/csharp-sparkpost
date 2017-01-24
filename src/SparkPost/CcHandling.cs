using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SparkPost
{
    internal static class CcHandling
    {
        internal static void DoStandardCcRewriting(Transmission transmission, IDictionary<string, object> result)
        {
            var recipients = transmission.Recipients;
            if (recipients.All(r => r.Type == RecipientType.To))
                return;

            if (recipients.Count(r => r.Type == RecipientType.To) != 1)
                throw new ArgumentException("There must be exactly one 'To' recipient if there are copied recipients.");

            var toRecipient = recipients.Single(r => r.Type == RecipientType.To);
            if (toRecipient.Address == null)
                throw new ArgumentException("'To' recipient has no address.");

            var toName = toRecipient.Address.Name;
            var toEmail = toRecipient.Address.Email;

            var ccRecipients = recipients.Where(r => r.Type == RecipientType.CC);
            if (ccRecipients.Any())
            {
                var ccHeader = GetCcHeader(ccRecipients);
                if (ccHeader != null)
                {
                    MakeSureThereIsAHeaderDefinedInTheRequest(result);
                    SetThisHeaderValue(result, "CC", ccHeader);
                }
            }

            var resultRecipients = (result["recipients"] as IEnumerable<IDictionary<string, object>>).ToList();
            SetFieldsOnRecipients(resultRecipients, toName, toEmail);
            result["recipients"] = resultRecipients;
        }

        private static void SetFieldsOnRecipients(IEnumerable<IDictionary<string, object>> recipients,
                string name, string email)
        {
            var addresses = recipients
                .Where(r => r.ContainsKey("address"))
                .Select(r => r["address"])
                .Cast<IDictionary<string, object>>();

            foreach (var address in addresses)
            {
                if (!String.IsNullOrWhiteSpace(name))
                    address["name"] = name;
                if (!String.IsNullOrWhiteSpace(email))
                    address["header_to"] = email;
            }
        }

        private static string GetCcHeader(IEnumerable<Recipient> recipients)
        {
            var listOfFormattedAddresses = recipients.Select(FormatAddress).Where(fa => !String.IsNullOrWhiteSpace(fa));
            return listOfFormattedAddresses.Any() ? String.Join(", ", listOfFormattedAddresses) : null;
        }

        private static string FormatAddress(Recipient recipient)
        {
            var address = recipient.Address;
            if (address == null)
                return null;

            if (String.IsNullOrWhiteSpace(address.Name))
                return address.Email;
            else if (String.IsNullOrWhiteSpace(address.Email))
                return null;
            else
            {
                var name = Regex.IsMatch(address.Name, @"[^\w ]") ? $"\"{address.Name}\"" : address.Name;
                return $"{name} <{address.Email}>";
            }
        }

        private static void MakeSureThereIsAHeaderDefinedInTheRequest(IDictionary<string, object> result)
        {
            if (result.ContainsKey("content") == false)
                result["content"] = new Dictionary<string, object>();

            var content = result["content"] as IDictionary<string, object>;
            if (content.ContainsKey("headers") == false)
                content["headers"] = new Dictionary<string, string>();
        }

        private static void SetThisHeaderValue(IDictionary<string, object> result, string key, string value)
        {
            ((IDictionary<string, string>) ((IDictionary<string, object>) result["content"])["headers"])
                [key] = value;
        }
    }
}