# EWS-Mail-Subscription-Processor
Simple console application that can use EWS to subscribe to an inbox and process messages. It downloads a .eml format of the message and stores it in Azure Blob Storage.

## Instructions to run
Open the web.config and add the following configuration values:
 * eml:AccountAddress - the email account you want to monitor (ex: me@mydomain.com)
 * eml:AccountPassword - the password for the account you want to monitor (ex: pass@word1)
 * abs:AccountName - the Azure Storage Account Name
 * abs:AccountKey - the Azure Storage Account Key
