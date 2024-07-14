using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CaptiveUrl
{
	public class Program
	{
		public static string url = "http://captive.apple.com/hotspot-detect.html";
		public static string text = "Success";
		public static async Task Main(string[] args)
		{
			if(args.Length != 0)
			{
				url = args[0];
				text = args[1];
			}
			Console.WriteLine("HttpClient: "+await GetCaptivePortalUrl3());
			Console.WriteLine("Http Client with redirection: " + await DetectCaptivePortal());
			string captivePortalUrl = GetCaptivePortalUrlCurl();
			Console.WriteLine(captivePortalUrl ?? "No captive portal detected");
		}
	public async static Task<string> GetCaptivePortalUrl3()
	{
		using (var client = new HttpClient())
    {
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                if (!content.Contains(text))
                {
                    return response.RequestMessage.RequestUri.ToString();
                }
            }
        }
        catch (HttpRequestException)
        {
            // Handle exception (e.g., no internet connection)
        }
    }
    return null;
	}
public static async Task<string> DetectCaptivePortal()
{
    var handler = new HttpClientHandler
    {
        AllowAutoRedirect = false // Disable automatic redirect following
    };

    using (HttpClient client = new HttpClient(handler))
    {
        try
        {
            var response = await client.GetAsync(url);
            
            // Check if we got a redirect status code
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                // We've been redirected, likely to a captive portal
                if (response.Headers.Location != null)
                {
                    return response.Headers.Location.ToString();
                }
            }
            
            // Even if we didn't get a redirect, check the content
            var content = await response.Content.ReadAsStringAsync();
            if (!content.Contains(text))
            {
                // The content doesn't match what we expect, might be a captive portal
                return response.RequestMessage.RequestUri.ToString();
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    return null; // No captive portal detected
}
public static string GetCaptivePortalUrlCurl()
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "curl",
        Arguments = $"-v {url}",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    try
    {
        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                Console.WriteLine("Failed to start curl process.");
                return null;
            }

            var output = new StringBuilder();
            while (!process.StandardOutput.EndOfStream)
            {
                output.AppendLine(process.StandardOutput.ReadLine());
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"curl process exited with code {process.ExitCode}");
                return null;
            }

            string response = output.ToString();
			Console.WriteLine(response);
            if (!response.Contains(text))
            {
				var match = Regex.Match(response, @"Location: (.+)");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }

                // If no Location header, check for URL in the body
                match = Regex.Match(response, @"<a href='(.+?)'>", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                // If still no match, check if the response indicates a captive portal
                if (!response.Contains(text))
                {
                    return "Captive portal detected, but redirect URL not found";
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

    return null;
}
		}
}
