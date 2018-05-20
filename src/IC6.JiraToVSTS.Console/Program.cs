using Atlassian.Jira;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IC6.JiraToVSTS.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            VssConnection connection = new VssConnection(new Uri(Settings.Default.VstsURL), new VssBasicCredential(string.Empty, Settings.Default.VstsPAT));
            using (WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>())
            {
                Jira jiraConn = Jira.CreateRestClient(Settings.Default.JiraURL, Settings.Default.JiraUser, Settings.Default.JiraPassword);

                var issues = (from i in jiraConn.Issues.Queryable
                              orderby i.Created
                              select i).ToList();

                foreach (var issue in issues)
                {
                    JsonPatchDocument document = new JsonPatchDocument();
                    string title = $"{issue.Key} - {issue.Summary}";
                    title = title.Substring(0, Math.Min(title.Length, 128));
                    document.Add(new JsonPatchOperation { Path = "/fields/System.Title", Value = title });
                    if (issue.Description != null)
                    {
                        document.Add(new JsonPatchOperation { Path = "/fields/System.Description", Value = issue.Description });
                    }

                    JiraUser user = jiraConn.Users.SearchUsersAsync(issue.Reporter).Result.FirstOrDefault();
                    if (user != null)
                    {
                        document.Add(new JsonPatchOperation { Path = "/fields/System.CreatedBy", Value = user.Email });
                    }

                    var x = issue.CustomFields["Story points"]?.Values.FirstOrDefault() ?? "";
                    if (x != "")
                    {
                        document.Add(new JsonPatchOperation { Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints", Value = x });

                    }

                    const string project = "Parrish Shoes";
                    const string workItemType = "User Story";
                    var workItem = witClient.CreateWorkItemAsync(document, project, workItemType).Result;
                }
            }
        }
    }
}
