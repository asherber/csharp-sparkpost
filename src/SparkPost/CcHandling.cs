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
            if (recipients.All(RecipientTypeIsTo))
                return;

            var toRecipient = recipients.FirstOrDefault(RecipientTypeIsTo);
            if (recipients.Count(RecipientTypeIsTo) != 1 || toRecipient.Address == null)
                return;

            var ccRecipients = recipients.Where(RecipientTypeIsCC);
            if (ccRecipients.Any())
            {
                var ccHeader = GetCcHeader(ccRecipients);
                if (!String.IsNullOrWhiteSpace(ccHeader))
                {
                    MakeSureThereIsAHeaderDefinedInTheRequest(result);
                    SetThisHeaderValue(result, "CC", ccHeader);
                }
            }

            var resultRecipients = (result["recipients"] as IEnumerable<IDictionary<string, object>>).ToList();
            SetFieldsOnRecipients(resultRecipients, toRecipient.Address.Name, toRecipient.Address.Email);
            result["recipients"] = resultRecipients;
        }

        private static bool RecipientTypeIsTo(Recipient recipient)
        {
            return recipient.Type == RecipientType.To;
        }

        private static bool RecipientTypeIsCC(Recipient recipient)
        {
            return recipient.Type == RecipientType.CC;
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