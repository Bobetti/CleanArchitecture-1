﻿using BlazorHero.CleanArchitecture.Client.Extensions;
using BlazorHero.CleanArchitecture.Client.Infrastructure.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BlazorHero.CleanArchitecture.Client.Shared
{
    public partial class MainLayout : IDisposable
    {

        [Inject]
        IJSRuntime _jsRuntime { get; set; }
        private string CurrentUserId { get; set; }
        private string FirstName { get; set; }
        private string SecondName { get; set; }
        private string Email { get; set; }
        private char FirstLetterOfName { get; set; }
        private async Task LoadDataAsync()
        {
            var state = await _stateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            if (user == null) return;
            if (user.Identity.IsAuthenticated)
            {
                CurrentUserId = user.GetUserId();
                this.FirstName = user.GetFirstName();
                if (this.FirstName.Length > 0)
                {
                    FirstLetterOfName = FirstName[0];
                }

            }

        }
        MudTheme currentTheme;
        private bool _drawerOpen = true;
        protected override async Task OnInitializedAsync()
        {
            _interceptor.RegisterEvent();
            currentTheme = await _preferenceManager.GetCurrentThemeAsync();
            hubConnection = hubConnection.TryInitialize(_navigationManager);
            await hubConnection.StartAsync();
            hubConnection.On<string, string>("ReceiveChatNotification", (message, userId) =>
            {
                if (CurrentUserId == userId)
                {
                    _jsRuntime.InvokeAsync<string>("PlayAudio", "notification");
                    _snackBar.Add(message, Severity.Info);
                    
                }
            });
            hubConnection.On("RegenerateTokens", async () =>
            {
                try
                {
                    var token = await _authenticationManager.TryForceRefreshToken();                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        _snackBar.Add("Refreshed Token.", Severity.Success);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    _snackBar.Add("You are Logged Out.", Severity.Error);
                    await _authenticationManager.Logout();
                    _navigationManager.NavigateTo("/");
                }
            });
        }
        void Logout()
        {
            string logoutConfirmationText = localizer["Logout Confirmation"];
            string logoutText = localizer["Logout"];
            var parameters = new DialogParameters();
            parameters.Add("ContentText", logoutConfirmationText);
            parameters.Add("ButtonText", logoutText);
            parameters.Add("Color", Color.Error);

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            _dialogService.Show<Dialogs.Logout>("Logout", parameters, options);
        }
        void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }
        async Task DarkMode()
        {
            bool isDarkMode = await _preferenceManager.ToggleDarkModeAsync();
            if (isDarkMode)
            {
                currentTheme = BlazorHeroTheme.DefaultTheme;
            }
            else
            {
                currentTheme = BlazorHeroTheme.DarkTheme;
            }
        }
        public void Dispose()
        {
            _interceptor.DisposeEvent();
            //_ = hubConnection.DisposeAsync();
        }

        private HubConnection hubConnection;
        public bool IsConnected => hubConnection.State == HubConnectionState.Connected;
    }
}
