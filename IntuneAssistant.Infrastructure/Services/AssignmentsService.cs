using System.Text;
using System.Text.Json;
using IntuneAssistant.Constants;
using IntuneAssistant.Enums;
using IntuneAssistant.Extensions;
using IntuneAssistant.Helpers;
using IntuneAssistant.Infrastructure.Interfaces;
using IntuneAssistant.Models;
using IntuneAssistant.Models.Group;
using Microsoft.Graph.Beta.Models.ODataErrors;
using Microsoft.IdentityModel.Tokens;

namespace IntuneAssistant.Infrastructure.Services;

public sealed class AssignmentsService : IAssignmentsService
{
    private readonly HttpClient _http = new();
    public async Task<List<CustomAssignmentsModel>?> GetConfigurationPolicyAssignmentsListAsync(string? accessToken,
        GroupModel? group, List<ConfigurationPolicyModel> configurationPolicies)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var policy in configurationPolicies)
            {
                var policyUrl =
                    $"/deviceManagement/configurationPolicies('{policy.Id}')/assignments";
                urlList.Add(policyUrl);
            }

            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithValue = result?.Responses.Where(r => r.Body.Value != null && r.Body.Value.Any()).ToList();
                var responsesWithNoValue = result?.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                if (responsesWithNoValue != null)
                    foreach (var nonAssigned in responsesWithNoValue)
                    {
                        var policyId = nonAssigned.Body.ODataContext.FetchIdFromContext();
                        var sourcePolicy = configurationPolicies.FirstOrDefault(p =>
                            p.Id == policyId);
                        var resourceType = ResourceTypes.ConfigurationPolicy.ToString();
                        if (sourcePolicy?.TemplateReference.TemplateDisplayName is not null)
                        {
                            resourceType = sourcePolicy.TemplateReference.TemplateDisplayName;
                        }
                        AssignmentsResponseModel resource = new AssignmentsResponseModel
                        {
                            Id = sourcePolicy?.Id,
                            DisplayName = sourcePolicy?.Name,
                            Assignments = new List<Assignment>()
                        };
                        var configurationPolicyAssignment =
                            resource.Assignments.FirstOrDefault()
                                .ToAssignmentModel(resource, resourceType);
                        results.Add(configurationPolicyAssignment);
                    }

                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = configurationPolicies.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    var resourceType = ResourceTypes.ConfigurationPolicy.ToString();
                    if (sourcePolicy?.TemplateReference.TemplateDisplayName is not null)
                    {
                        resourceType = sourcePolicy.TemplateReference.TemplateDisplayName;
                    }
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.Name,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, resourceType);
                            results.Add(configurationPolicyAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, resourceType);
                            results.Add(configurationPolicyAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDeviceManagementScriptsAssignmentsListAsync(string? accessToken,
        GroupModel? group, List<DeviceScriptsModel> deviceScripts)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var script in deviceScripts)
            {
                var scriptUrl =
                    $"/deviceManagement/deviceManagementScripts('{script.Id}')?$expand=assignments";
                urlList.Add(scriptUrl);
            }
            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithValue = result?.Responses.Where(r => r.Body.Value != null && r.Body.Value.Any()).ToList();
                var responsesWithNoValue = result?.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                if (responsesWithNoValue != null)
                    foreach (var nonAssigned in responsesWithNoValue)
                    {
                        var policyId = nonAssigned.Body.AssignmentsOdataContext;
                        var sourceScript = deviceScripts.FirstOrDefault(p =>
                            p.Id == policyId.FetchIdFromContext());
                        AssignmentsResponseModel resource = new AssignmentsResponseModel
                        {
                            Id = sourceScript?.Id,
                            DisplayName = sourceScript?.DisplayName,
                            Assignments = new List<Assignment>()
                        };
                        var configurationPolicyAssignment =
                            resource.Assignments.FirstOrDefault()
                                .ToAssignmentModel(resource, ResourceTypes.DeviceManagementScript.ToString());
                        results.Add(configurationPolicyAssignment);
                    }

                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = deviceScripts.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceManagementScript.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceManagementScript.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDeviceShellScriptsAssignmentsListAsync(string? accessToken, GroupModel? group, List<DeviceScriptsModel> deviceShellScripts)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var script in deviceShellScripts)
            {
                var scriptUrl =
                    $"/deviceManagement/deviceShellScripts('{script.Id}')?$expand=assignments";
                urlList.Add(scriptUrl);
            }
            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithValue = result?.Responses.Where(r => r.Body.Value != null && r.Body.Value.Any()).ToList();
                var responsesWithNoValue = result?.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                if (responsesWithNoValue != null)
                    foreach (var nonAssigned in responsesWithNoValue)
                    {
                        var policyId = nonAssigned.Body.ODataContext.FetchIdFromContext();
                        var sourceScript = deviceShellScripts.FirstOrDefault(p =>
                            p.Id == policyId);
                        AssignmentsResponseModel resource = new AssignmentsResponseModel
                        {
                            Id = sourceScript?.Id,
                            DisplayName = sourceScript?.DisplayName,
                            Assignments = new List<Assignment>()
                        };
                        var configurationPolicyAssignment =
                            resource.Assignments.FirstOrDefault()
                                .ToAssignmentModel(resource, ResourceTypes.MacOsShellScript.ToString());
                        results.Add(configurationPolicyAssignment);
                    }

                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = deviceShellScripts.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsShellScript.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsShellScript.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDeviceConfigurationsAssignmentsListAsync(string? accessToken, GroupModel? group, List<DeviceConfigurationModel> configurations)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var policy in configurations)
            {
                var policyUrl =
                    $"/deviceManagement/deviceConfigurations('{policy.Id}')/assignments";
                urlList.Add(policyUrl);
            }

            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithNoValue = result.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                foreach (var nonAssigned in responsesWithNoValue)
                {
                    var policyId = nonAssigned.Body.ODataContext.FetchIdFromContext();
                    var sourcePolicy = configurations.FirstOrDefault(p =>
                        nonAssigned != null &&
                        p.Id == policyId);
                    var resourceType = ResourceHelper.GetResourceTypeFromOdata(sourcePolicy.OdataType);
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = new List<Assignment>()
                    };
                    var configurationsAssignment =
                        resource.Assignments.FirstOrDefault().ToAssignmentModel(resource, resourceType);
                    results.Add(configurationsAssignment);
                }
                var responsesWithValue = result.Responses.Where(r => r.Body.Value.Any()).ToList();
                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = configurations.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    var resourceType = ResourceHelper.GetResourceTypeFromOdata(sourcePolicy.OdataType);
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var DeviceConfigurationAssignment =
                                assignment.ToAssignmentModel(resource, resourceType);
                            results.Add(DeviceConfigurationAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var DeviceConfigurationAssignment =
                                assignment.ToAssignmentModel(resource, resourceType);
                            results.Add(DeviceConfigurationAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetGroupPolicyConfigurationsAssignmentsListAsync(string? accessToken, GroupModel? group, List<GroupPolicyConfigurationModel> groupPolicies)
    {
         _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var policy in groupPolicies)
            {
                var policyUrl =
                    $"/deviceManagement/groupPolicyConfigurations('{policy.Id}')/assignments";
                urlList.Add(policyUrl);
            }

            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithNoValue = result.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                foreach (var nonAssigned in responsesWithNoValue)
                {
                    var policyId = nonAssigned.Body.ODataContext.FetchIdFromContext();
                    var sourcePolicy = groupPolicies.FirstOrDefault(p =>
                        nonAssigned != null &&
                        p.Id == policyId);
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = new List<Assignment>()
                    };
                    var assignmentResponse =
                        resource.Assignments.FirstOrDefault().ToAssignmentModel(resource, ResourceTypes.GroupPolicyConfiguration.ToString());
                    results.Add(assignmentResponse);
                }
                var responsesWithValue = result.Responses.Where(r => r.Body.Value.Any()).ToList();
                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = groupPolicies.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    if (sourcePolicy is null)
                    {
                        var sourceId = assignmentResponse.Select(a => a.Id.Split('_')[0]);
                        sourcePolicy = groupPolicies.FirstOrDefault(p =>
                            assignmentResponse != null &&
                            p.Id == sourceId.FirstOrDefault());
                    }
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.GroupPolicyConfiguration.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.GroupPolicyConfiguration.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetHealthScriptsAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.DeviceHealthScriptsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var healthScriptAssigment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceHealthScript.ToString());
                            results.Add(healthScriptAssigment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var healthScriptAssigment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceHealthScript.ToString());
                            results.Add(healthScriptAssigment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetAutoPilotAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.WindowsAutopilotDeploymentProfilesUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var autoPilotAssigment = assignment.ToAssignmentModel(resource,
                                ResourceTypes.WindowsAutopilotDeploymentProfile.ToString());
                            results.Add(autoPilotAssigment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var autoPilotAssigment = assignment.ToAssignmentModel(resource,
                                ResourceTypes.WindowsAutopilotDeploymentProfile.ToString());
                            results.Add(autoPilotAssigment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetMobileAppAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response =
                await _http.GetAsync(
                    $"{GraphUrls.MobileAppsUrl}?$expand=assignments($select=id,target)&$select=id,displayname,description");
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var mobileAppAssigment = assignment.ToAssignmentModel(resource, ResourceTypes.MobileApp.ToString());
                            results.Add(mobileAppAssigment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var mobileAppAssigment = assignment.ToAssignmentModel(resource, ResourceTypes.MobileApp.ToString());
                            results.Add(mobileAppAssigment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetManagedApplicationAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.ManagedAppPoliciesUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var managedAppResult =
                                assignment.ToAssignmentModel(resource, ResourceTypes.ManagedAppPolicy.ToString());
                            results.Add(managedAppResult);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var managedAppResult =
                                assignment.ToAssignmentModel(resource, ResourceTypes.ManagedAppPolicy.ToString());
                            results.Add(managedAppResult);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetTargetedAppConfigurationsAssignmentsByGroupListAsync(
        string? accessToken, GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.TargetedManagedAppConfigurationsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var targetedAppAssigment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.AppConfigurationPolicy.ToString());
                            results.Add(targetedAppAssigment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var targetedAppAssigment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.AppConfigurationPolicy.ToString());
                            results.Add(targetedAppAssigment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetAppProtectionAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            try
            {
                var response = await _http.GetAsync(GraphUrls.WindowsManagedAppProtectionsUrl);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                        CustomJsonOptions.Default());
                if (result?.Value is not null)
                {
                    foreach (var resource in result.Value)
                    {
                        if (group is null)
                        {
                            foreach (var assignment in resource.Assignments)
                            {
                                var appProtectionAssigment = assignment.ToAssignmentModel(resource,
                                    ResourceTypes.WindowsManagedAppProtection.ToString());
                                results.Add(appProtectionAssigment);
                            }
                        }
                        else
                            foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                            {
                                var appProtectionAssigment = assignment.ToAssignmentModel(resource,
                                    ResourceTypes.WindowsManagedAppProtection.ToString());
                                results.Add(appProtectionAssigment);
                            }
                    }
                }
            }
            catch (ODataError ex)
            {
                Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
                return null;
            }

            // iOS app protection
            try
            {
                var response = await _http.GetAsync(GraphUrls.IosManagedAppProtectionsUrl);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                        CustomJsonOptions.Default());
                if (result?.Value is not null)
                {
                    foreach (var resource in result.Value)
                    {
                        if (group is null)
                        {
                            foreach (var assignment in resource.Assignments)
                            {
                                var iosAppAssigment =
                                    assignment.ToAssignmentModel(resource, ResourceTypes.IosManagedAppProtection.ToString());
                                results.Add(iosAppAssigment);
                            }
                        }
                        else
                            foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                            {
                                var iosAppAssigment =
                                    assignment.ToAssignmentModel(resource, ResourceTypes.IosManagedAppProtection.ToString());
                                results.Add(iosAppAssigment);
                            }
                    }
                }
            }
            catch (ODataError ex)
            {
                Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
                return null;
            }

            // Android app protection
            try
            {
                var response = await _http.GetAsync(GraphUrls.AndroidManagedAppProtectionsUrl);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                        CustomJsonOptions.Default());
                if (result?.Value is not null)
                {
                    foreach (var resource in result.Value)
                    {
                        if (group is null)
                        {
                            foreach (var assignment in resource.Assignments)
                            {
                                var androidAppAssigment = assignment.ToAssignmentModel(resource,
                                    ResourceTypes.AndroidManagedAppProtection.ToString());
                                results.Add(androidAppAssigment);
                            }
                        }
                        else
                            foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                            {
                                var androidAppAssigment = assignment.ToAssignmentModel(resource,
                                    ResourceTypes.AndroidManagedAppProtection.ToString());
                                results.Add(androidAppAssigment);
                            }
                    }
                }
            }
            catch (ODataError ex)
            {
                Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
                return null;
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching autopilot policies: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>> GetCompliancePoliciesAssignmentsListAsync(string? accessToken,
        GroupModel? group, List<CompliancePolicyModel> compliancePolicies)
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var results = new List<CustomAssignmentsModel>();
        try
        {
            var urlList = new List<string>();
            foreach (var policy in compliancePolicies)
            {
                var policyUrl =
                    $"/deviceManagement/deviceCompliancePolicies('{policy.Id}')/assignments";
                urlList.Add(policyUrl);
            }

            var batchRequestBody = GraphBatchHelper.CreateUrlListBatchOutput(urlList);
            foreach (var requestBody in batchRequestBody)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
                var responseStream = await response.Content.ReadAsStreamAsync();
                var result =
                    await JsonSerializer
                        .DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments<Assignment>>>(
                            responseStream,
                            CustomJsonOptions.Default());
                var responsesWithNoValue = result.Responses.Where(r => r.Body.Value.IsNullOrEmpty()).ToList();
                foreach (var nonAssigned in responsesWithNoValue)
                {
                    var policyId = nonAssigned.Body.ODataContext.FetchIdFromContext();
                    var sourcePolicy = compliancePolicies.FirstOrDefault(p =>
                        nonAssigned != null &&
                        p.Id == policyId);
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = new List<Assignment>()
                    };
                    var assignmentResponse =
                        resource.Assignments.FirstOrDefault().ToAssignmentModel(resource, ResourceTypes.WindowsCompliancePolicy.ToString());
                    results.Add(assignmentResponse);
                }
                var responsesWithValue = result.Responses.Where(r => r.Body.Value.Any()).ToList();
                foreach (var assignmentResponse in responsesWithValue.Select(r => r.Body.Value))
                {
                    var sourcePolicy = compliancePolicies.FirstOrDefault(p =>
                        assignmentResponse != null &&
                        p.Id == assignmentResponse.Select(a => a.SourceId).FirstOrDefault());
                    AssignmentsResponseModel resource = new AssignmentsResponseModel
                    {
                        Id = sourcePolicy?.Id,
                        OdataType = sourcePolicy.OdataType,
                        DisplayName = sourcePolicy?.DisplayName,
                        Assignments = assignmentResponse.Select(a => a).ToList()
                    };
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsCompliancePolicy.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var configurationPolicyAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsCompliancePolicy.ToString());
                            results.Add(configurationPolicyAssignment);
                        }
                }
            }
            return results;
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
        }
        return null;
    }

    public async Task<List<CustomAssignmentsModel>?> GetUpdateRingsAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.UpdateRingsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var updateRingAssignmentInfo =
                                assignment.ToAssignmentModel(resource, ResourceTypes.UpdateRingConfiguration.ToString());
                            results.Add(updateRingAssignmentInfo);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var updateRingAssignmentInfo =
                                assignment.ToAssignmentModel(resource, ResourceTypes.UpdateRingConfiguration.ToString());
                            results.Add(updateRingAssignmentInfo);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetFeatureUpdatesAssignmentsByGroupListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.WindowsFeatureUpdatesUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var featureUpdateAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsFeatureUpdate.ToString());
                            results.Add(featureUpdateAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var updateRingAssignmentInfo =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsFeatureUpdate.ToString());
                            results.Add(updateRingAssignmentInfo);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetWindowsDriverUpdatesAssignmentsByGroupListAsync(
        string? accessToken, GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.WindowsDriverUpdatesUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var driverUpdateAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsDriverUpdate.ToString());
                            results.Add(driverUpdateAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var updateRingAssignmentInfo =
                                assignment.ToAssignmentModel(resource, ResourceTypes.WindowsDriverUpdate.ToString());
                            results.Add(updateRingAssignmentInfo);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching devices: " + ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetMacOsShellScriptsAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.MacOsShellScripts);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var macosShellScriptAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsShellScript.ToString());
                            results.Add(macosShellScriptAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var macosShellScriptAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsShellScript.ToString());
                            results.Add(macosShellScriptAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching macOS shell script assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDiskEncryptionAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var globalResponse = await _http.GetAsync(GraphUrls.DiskEncryptionPoliciesUrl);
            var globalResponseStream = await globalResponse.Content.ReadAsStreamAsync();
            var batchRequestBody = GraphBatchHelper.IntentHelper.CreateOutput(globalResponseStream);
            var content = new StringContent(batchRequestBody, Encoding.UTF8, "application/json");
            var diskEncryptionResponse = await _http.PostAsync(AppConfiguration.GRAPH_BATCH_URL, content);
            var responseStream = await diskEncryptionResponse.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphBatchResponse<InnerResponseForAssignments>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Responses is not null)
            {
                foreach (var resource in result.Responses)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Body.Assignments)
                        {
                            var diskEncryptionAssignment =
                                assignment.ToAssignmentModel(resource.Body, ResourceTypes.DiskEncryptionPolicy.ToString());
                            results.Add(diskEncryptionAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Body.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var diskEncryptionAssignment =
                                assignment.ToAssignmentModel(resource.Body, ResourceTypes.DiskEncryptionPolicy.ToString());
                            results.Add(diskEncryptionAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching macOS shell script assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetPlatformScriptsAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.PlatformScriptsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var platformScriptAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.PlatformScripts.ToString());
                            results.Add(platformScriptAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var platformScriptAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.PlatformScripts.ToString());
                            results.Add(platformScriptAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching macOS shell script assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDevicePlatformRestrictionsAssignmentListAsync(
        string? accessToken, GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.DevicePlatformRestrictionsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var enrollmentRestrictionsAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DevicePlatformRestriction.ToString());
                            results.Add(enrollmentRestrictionsAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var enrollmentRestrictionsAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DevicePlatformRestriction.ToString());
                            results.Add(enrollmentRestrictionsAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching device platform restriction assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetDeviceLimitRestrictionsAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.DeviceLimitRestrictionsUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var limitRestrictionsAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceLimitRestriction.ToString());
                            results.Add(limitRestrictionsAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var limitRestrictionsAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.DeviceLimitRestriction.ToString());
                            results.Add(limitRestrictionsAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching device platform restriction assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetMacOsCustomAttributesAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.MacOsCustomAttributesScripts);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var macOsCustomAttributesAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsCustomAttributes.ToString());
                            results.Add(macOsCustomAttributesAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var macOsCustomAttributesAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.MacOsCustomAttributes.ToString());
                            results.Add(macOsCustomAttributesAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching macOS custom attributes assignments: " +
                              ex.ToMessage());
            return null;
        }

        return results;
    }

    public async Task<List<CustomAssignmentsModel>?> GetIosLobAppProvisioningAssignmentListAsync(string? accessToken,
        GroupModel? group)
    {
        var results = new List<CustomAssignmentsModel>();
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        try
        {
            var response = await _http.GetAsync(GraphUrls.IosLobAppProvisioningUrl);
            var responseStream = await response.Content.ReadAsStreamAsync();
            var result =
                await JsonSerializer.DeserializeAsync<GraphValueResponse<AssignmentsResponseModel>>(responseStream,
                    CustomJsonOptions.Default());
            if (result?.Value is not null)
            {
                foreach (var resource in result.Value)
                {
                    if (group is null)
                    {
                        foreach (var assignment in resource.Assignments)
                        {
                            var iosLobAppAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.IosLobAppConfiguration.ToString());
                            results.Add(iosLobAppAssignment);
                        }
                    }
                    else
                        foreach (var assignment in resource.Assignments.Where(g => g.Target.GroupId == group.Id))
                        {
                            var iosLobAppAssignment =
                                assignment.ToAssignmentModel(resource, ResourceTypes.IosLobAppConfiguration.ToString());
                            results.Add(iosLobAppAssignment);
                        }
                }
            }
        }
        catch (ODataError ex)
        {
            Console.WriteLine("An exception has occurred while fetching iOS Lob apps assignments: " + ex.ToMessage());
            return null;
        }

        return results;
    }
}