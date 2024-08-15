using Confab.Data;
using Confab.Data.DatabaseModels;
using Confab.Exceptions;
using Confab.Models;
using Confab.Models.AdminPanel.CommentSettings;
using Confab.Models.AdminPanel.Emails;
using Confab.Models.AdminPanel.PageDetection;
using Confab.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Bcpg.Sig;
using System;
using System.Text.RegularExpressions;
using static Confab.Data.DatabaseModels.CommentLocationSchema;
using static Confab.Models.AdminPanel.CommentSettings.LocalCommentSettings;

namespace Confab.Services
{
    public class SettingsService: ISettingsService
    {
        public async Task<GlobalCommentSettings> GetGlobalCommentSettings(DataContext dbCtx)
        {
            GlobalSettingsSchema settingsObj = await dbCtx.GlobalSettings.SingleAsync();

            return new GlobalCommentSettings
            {
                CommentingStatus = settingsObj.CommentingStatus,
                VotingEnabled = settingsObj.VotingEnabled,
                AccountCreationEnabled = settingsObj.AccountCreationEnabled,
                AccountLoginEnabled = settingsObj.AccountLoginEnabled,
            };
        }

        public async Task SetGlobalCommentSettings(DataContext dbCtx, GlobalCommentSettings newSettings)
        {
            GlobalSettingsSchema settingsObj = await dbCtx.GlobalSettings.SingleAsync();

            settingsObj.CommentingStatus = newSettings.CommentingStatus ?? settingsObj.CommentingStatus;
            settingsObj.VotingEnabled = newSettings.VotingEnabled ?? settingsObj.VotingEnabled;
            settingsObj.AccountCreationEnabled = newSettings.AccountCreationEnabled ?? settingsObj.AccountCreationEnabled;
            settingsObj.AccountLoginEnabled = newSettings.AccountLoginEnabled ?? settingsObj.AccountLoginEnabled;

            dbCtx.GlobalSettings.Update(settingsObj);
            await dbCtx.SaveChangesAsync();
        }

        public async Task<LocalCommentSettings> GetLocalCommentSettings(ICommentLocationService locationService, CommentLocation commentLocation, DataContext dbCtx)
        {
            CommentLocationSchema locationObj = await locationService.GetLocation(dbCtx, commentLocation?.Location);

            return new LocalCommentSettings
            {
                CommentingStatus = locationObj != null ? (CommentingStatusResponse)locationObj.LocalStatus : CommentingStatusResponse.Uninitialised,
                VotingStatus = locationObj != null ? locationObj.LocalVotingEnabled : null,
                EditingStatus = locationObj != null ? locationObj.LocalEditingEnabled : null,
            };
        }

        public async Task SetLocalCommentSettings(SetLocalCommentSettings newSettings, ICommentLocationService locationService, DataContext dbCtx)
        {
            CommentLocationSchema locationObj = await locationService.GetLocation(dbCtx, newSettings?.Location);
            if (locationObj == null)
            {
                locationObj = await locationService.CreateNewLocation(dbCtx, newSettings.Location);
            } 
            
            locationObj.LocalStatus = newSettings.CommentingStatus ?? locationObj.LocalStatus;
            locationObj.LocalVotingEnabled = newSettings.VotingStatus ?? locationObj.LocalVotingEnabled;
            locationObj.LocalEditingEnabled = newSettings.EditingStatus ?? locationObj.LocalEditingEnabled;

            dbCtx.Update(locationObj);
            await dbCtx.SaveChangesAsync();
        }

        public async Task SignOutAllUsers(DataContext dbCtx)
        {
            GlobalSettingsSchema settingsObj = await dbCtx.GlobalSettings.SingleAsync();

            settingsObj.UserAuthJwtValidityStart = DateTime.UtcNow;

            dbCtx.GlobalSettings.Update(settingsObj);
            await dbCtx.SaveChangesAsync();
        }

        public async Task<EmailSettings> GetEmailSettings(CommentLocation locationData, ICommentLocationService locationService, DataContext dbCtx)
        {
            CommentLocationSchema locationObj = null;
            try
            {
                locationObj = await locationService.GetLocation(dbCtx, locationData?.Location);
            }
            catch {}

            GlobalSettingsSchema globalSettings = await dbCtx.GlobalSettings.SingleAsync();

            return new EmailSettings
            {
                AdminNotifGlobal = globalSettings.AdminNotifGlobal,
                AdminNotifEditGlobal = globalSettings.AdminNotifEditGlobal,
                AdminNotifLocal = locationObj?.AdminNotifLocal,
                AdminNotifEditLocal = locationObj?.AdminNotifEditLocal,
                UserNotifGlobal = globalSettings.UserNotifGlobal,
                UserNotifLocal = locationObj?.UserNotifLocal,
            };
        }

        public async Task SetEmailSettings(EmailSettings newSettings, ICommentLocationService locationService, DataContext dbCtx)
        {
            CommentLocationSchema locationObj = null;
            try
            {
                locationObj = await locationService.GetLocation(dbCtx, newSettings?.Location);
            }
            catch { }

            if (locationObj == null && (newSettings.AdminNotifLocal != null || newSettings.UserNotifLocal != null))
            {
                throw new UninitialisedLocationException();
            }

            GlobalSettingsSchema globalSettings = await dbCtx.GlobalSettings.SingleAsync();

            if (locationObj != null)
            {
                locationObj.AdminNotifLocal = newSettings.AdminNotifLocal ?? locationObj.AdminNotifLocal;
                locationObj.AdminNotifEditLocal = newSettings.AdminNotifEditLocal ?? locationObj.AdminNotifEditLocal;
                locationObj.UserNotifLocal = newSettings.UserNotifLocal ?? locationObj.UserNotifLocal;

                dbCtx.CommentLocations.Update(locationObj);
            }
            globalSettings.AdminNotifGlobal = newSettings.AdminNotifGlobal ?? globalSettings.AdminNotifGlobal;
            globalSettings.AdminNotifEditGlobal = newSettings.AdminNotifEditGlobal ?? globalSettings.AdminNotifEditGlobal;
            globalSettings.UserNotifGlobal = newSettings.UserNotifGlobal ?? globalSettings.UserNotifGlobal;

            dbCtx.GlobalSettings.Update(globalSettings);

            await dbCtx.SaveChangesAsync();
        }
    }
}
