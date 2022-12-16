﻿using System.Net.Http;
using System.Net.Http.Json;

using Vapour.Shared.Common.Core;
using Vapour.Shared.Configuration.Profiles.Schema;

namespace Vapour.Client.ServiceClients;

public sealed partial class ProfileServiceClient
{
    public async Task<IProfile> CreateNewProfile()
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage result =
            await client.PostAsync(new Uri($"{Constants.HttpUrl}/api/profile/new", UriKind.Absolute), null);
        if (result.IsSuccessStatusCode)
        {
            return await result.Content.ReadFromJsonAsync<ProfileItem>();
        }

        throw new Exception($"Could not get new {result.ReasonPhrase}");
    }

    public async Task DeleteProfile(Guid id)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage result =
            await client.DeleteAsync(new Uri($"{Constants.HttpUrl}/api/profile/delete/{id}", UriKind.Absolute));

        if (!result.IsSuccessStatusCode)
        {
            throw new Exception($"Could not delete profile {result.ReasonPhrase}");
        }

        ProfileList.Remove(ProfileList.Single(i => i.Id == id));
    }

    public async Task<IProfile> SaveProfile(IProfile profile)
    {
        using HttpClient client = _httpClientFactory.CreateClient();

        HttpResponseMessage result = await client.PostAsync(
            new Uri($"{Constants.HttpUrl}/api/profile/save", UriKind.Absolute),
            JsonContent.Create(profile));

        if (result.IsSuccessStatusCode)
        {
            ProfileItem savedProfile = await result.Content.ReadFromJsonAsync<ProfileItem>();

            IProfile existing = ProfileList.SingleOrDefault(i => i.Id == savedProfile.Id);
            if (existing != null)
            {
                int existingIndex = ProfileList.IndexOf(existing);
                ProfileList[existingIndex] = savedProfile;
            }
            else
            {
                ProfileList.Add(savedProfile);
            }

            return savedProfile;
        }

        throw new Exception($"Could not save profile {result.ReasonPhrase}");
    }
}