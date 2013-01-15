using System;
using osum.GameModes.SongSelect;
using osum.Graphics.Skins;
using OpenTK;
using osum.Graphics.Sprites;
using OpenTK.Graphics.ES11;
using OpenTK.Graphics;
using osum.Helpers;
using System.Diagnostics;
using osum.Audio;
using osum.UI;
using osum.Resources;
using osu_common.Libraries.NetLib;
using osum.Graphics.Renderers;


#if iOS
using MonoTouch.Accounts;
using MonoTouch.Foundation;
using osum.Support.iPhone;
#endif

namespace osum.GameModes.Options
{
    public class Options : GameMode
    {
        BackButton s_ButtonBack;
        SpriteManagerDraggable smd = new SpriteManagerDraggable()
        {
            Scrollbar = true
        };

        private SliderControl soundEffectSlider;
        private SliderControl universalOffsetSlider;

        SpriteManager topMostSpriteManager = new SpriteManager();

        internal static float ScrollPosition;

        public override void Initialize()
        {
            s_Header = new pSprite(TextureManager.Load(OsuTexture.options_header), new Vector2(0, 0));
            s_Header.OnClick += delegate { };
            topMostSpriteManager.Add(s_Header);

            pDrawable background =
                new pSprite(TextureManager.Load(OsuTexture.songselect_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre,
                            ClockTypes.Mode, Vector2.Zero, 0, true, new Color4(56, 56, 56, 255));
            background.AlphaBlend = false;
            spriteManager.Add(background);

            s_ButtonBack = new BackButton(delegate { Director.ChangeMode(OsuMode.MainMenu); }, Director.LastOsuMode == OsuMode.MainMenu);
            smd.AddNonDraggable(s_ButtonBack);

            if (MainMenu.InitializeBgm())
                AudioEngine.Music.Play();

            const int header_x_offset = 60;

            float button_x_offset = GameBase.BaseSize.X / 2;

            int vPos = 70;

            pText text = new pText(LocalisationManager.GetString(OsuString.About), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            pButton button = new pButton(LocalisationManager.GetString(OsuString.Credits), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
            {
                Director.ChangeMode(OsuMode.Credits);
            });
            smd.Add(button);

            vPos += 70;

            button = new pButton(LocalisationManager.GetString(OsuString.OnlineHelp), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
            {
                GameBase.Instance.ShowWebView("http://www.osustream.com/help/", "Online Help");
            });

            smd.Add(button);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.DifficultySettings), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            buttonFingerGuides = new pButton(LocalisationManager.GetString(OsuString.UseFingerGuides), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
            {
                DisplayFingerGuideDialog();
            });
            smd.Add(buttonFingerGuides);

            vPos += 70;

            buttonEasyMode = new pButton(LocalisationManager.GetString(OsuString.DefaultToEasyMode), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
            {
                DisplayEasyModeDialog();
            });
            smd.Add(buttonEasyMode);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.Audio), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 80;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.EffectVolume), AudioEngine.Effect.Volume, new Vector2(button_x_offset - 15, vPos),
                delegate(float v)
                {
                    AudioEngine.Effect.Volume = v;
                    if (Clock.ModeTime / 200 != lastEffectSound)
                    {
                        lastEffectSound = Clock.ModeTime / 200;
                        switch (lastEffectSound % 4)
                        {
                            case 0:
                                AudioEngine.PlaySample(OsuSamples.HitNormal);
                                break;
                            case 1:
                            case 3:
                                AudioEngine.PlaySample(OsuSamples.HitWhistle);
                                break;
                            case 2:
                                AudioEngine.PlaySample(OsuSamples.HitFinish);
                                break;

                        }
                    }
                });
            smd.Add(soundEffectSlider);

            vPos += 60;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.MusicVolume), AudioEngine.Music.MaxVolume, new Vector2(button_x_offset - 15, vPos),
                delegate(float v) { AudioEngine.Music.MaxVolume = v; });
            smd.Add(soundEffectSlider);

            vPos += 60;

            universalOffsetSlider = new SliderControl(LocalisationManager.GetString(OsuString.UniversalOffset), (Clock.USER_OFFSET + 32) / 64f , new Vector2(button_x_offset - 15, vPos),
                delegate(float v) {
                    GameBase.Config.SetValue<int>("offset", (Clock.USER_OFFSET = (int)((v - 0.5f) * 64)));
                    if (universalOffsetSlider != null) //will be null on first run.
                        universalOffsetSlider.Text.Text = Clock.USER_OFFSET.ToString() + "ms";
                 });
            smd.Add(universalOffsetSlider);

            vPos += 40;

            text = new pText(LocalisationManager.GetString(OsuString.UniversalOffsetDetails), 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };
            text.Field = FieldTypes.StandardSnapTopCentre;
            text.Origin = OriginTypes.TopCentre;
            text.TextAlignment = TextAlignment.Centre;
            text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
            text.TextBounds.X = 600;
            smd.Add(text);

            vPos += (int)text.MeasureText().Y + 50;

            text = new pText(LocalisationManager.GetString(OsuString.OnlineOptions), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 80;

            if (!GameBase.HasAuth)
            {
                button = new pButton(LocalisationManager.GetString(OsuString.TwitterLink), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, HandleTwitterAuth);
                smd.Add(button);

                vPos += 40;

                text = new pText(LocalisationManager.GetString(OsuString.Twitter), 24, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };

                text.Field = FieldTypes.StandardSnapTopCentre;
                text.Origin = OriginTypes.TopCentre;
                text.TextAlignment = TextAlignment.Centre;
                text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
                text.TextBounds.X = 600;

                smd.Add(text);

                /*vPos += (int)text.MeasureText().Y + 50;

                button = new pButton(LocalisationManager.GetString(OsuString.GuestUsername), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
                {
#if iOS
                    TextInputNotification tin = new TextInputNotification(LocalisationManager.GetString(OsuString.ChooseUsername), GameBase.Config.GetValue<string>("username", "Guest"), delegate(bool yes)
                        {
                            if (yes)
                            {
                                GameBase.Config.SetValue<string>("username", tin.Text);
                                GameBase.Config.SaveConfig();
                            }
                        });
#endif
                });
                smd.Add(button);*/
            }
            else
            {
                button = new pButton(string.Format(LocalisationManager.GetString(OsuString.TwitterUnlink), GameBase.Config.GetValue<string>("username", null)), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate
                {
                    StringNetRequest nr = new StringNetRequest("http://osustream.com/twitter/disconnect.php?udid="
                        + GameBase.Instance.DeviceIdentifier + "&cc=" + GameBase.Config.GetValue<string>("hash", null));
                    nr.onFinish += delegate(string _result, Exception e)
                    {
                        GameBase.GloballyDisableInput = false;

                        if (e != null || _result != "success")
                            GameBase.Notify(LocalisationManager.GetString(OsuString.TwitterLinkError));
                        else
                        {
                            GameBase.Config.SetValue<string>("username", null);
                            GameBase.Config.SetValue<string>("hash", null);
                            GameBase.Config.SetValue<string>("twitterId", null);
                            GameBase.Config.SaveConfig();

                            Director.ChangeMode(Director.CurrentOsuMode);
                        }
                    };

                    GameBase.GloballyDisableInput = true;

                    NetManager.AddRequest(nr);
                });

                smd.Add(button);
            }

            UpdateButtons();

            vPos += 50;

            smd.ScrollTo(ScrollPosition);
        }

#if iOS
        ACAccountStore accountStore;
        private void HandleTwitterAuth(object sender, EventArgs args)
        {
            if (HardwareDetection.RunningiOS5OrHigher)
            {
                //if we are running iOS5 or later, we can use the in-built API for handling twitter authentication.
                accountStore = new ACAccountStore();
                accountStore.RequestAccess(accountStore.FindAccountType(ACAccountType.Twitter), retrievedAccounts);
            }
            else
            {
                HandleTwitterOAuth();
            }
        }

        private void HandleTwitterOAuth()
        {
            GameBase.Instance.ShowWebView("http://osustream.com/twitter/connect.php?udid=" + GameBase.Instance.DeviceIdentifier,
                LocalisationManager.GetString(OsuString.TwitterLink),
                delegate(string url)
                {
                    if (url.StartsWith("finished://"))
                    {
                        string[] split = url.Replace("finished://", "").Split('/');

                        GameBase.Config.SetValue<string>("username", split[0]);
                        GameBase.Config.SetValue<string>("hash", split[1]);
                        GameBase.Config.SetValue<string>("twitterId", split[2]);
                        GameBase.Config.SaveConfig();

                        Director.ChangeMode(Director.CurrentOsuMode);
                        return true;
                    }
                    return false;
                });
        }

        private void retrievedAccounts(bool granted, NSError error)
        {
            ACAccount[] accounts = accountStore.FindAccounts(accountStore.FindAccountType(ACAccountType.Twitter));

            if (!granted || error != null || accounts == null || accounts.Length == 0)
                handleManualAuth();
            else
                tryNextAccount();
        }

        private void handleManualAuth()
        {
            if (accountStore != null)
            {
                accountStore.Dispose();
                accountStore = null;
            }

            Notification n = new Notification(LocalisationManager.GetString(OsuString.AccessNotGranted),
                                              LocalisationManager.GetString(OsuString.AccessNotGrantedDetails),
                                              NotificationStyle.YesNo,
                                             delegate(bool resp) {
                if (resp) HandleTwitterOAuth();
            });
            GameBase.Notify(n);
        }

        private void tryNextAccount(int index = 0)
        {
                ACAccount[] accounts = accountStore.FindAccounts(accountStore.FindAccountType(ACAccountType.Twitter));
                ACAccount account = accounts[index];

                Notification n = new Notification(
                                    LocalisationManager.GetString(OsuString.TwitterLinkQuestion),
                                    string.Format(LocalisationManager.GetString(OsuString.TwitterLinkQuestionDetails), account.Username),
                                    NotificationStyle.YesNo,
                                    delegate(bool resp) {
                    if (!resp)
                    {
                        if (index == accounts.Length - 1) //exhausted our options.
                            handleManualAuth();
                        else
                            tryNextAccount(index + 1);
                        return;
                    }

                    NSDictionary properties = account.GetDictionaryOfValuesFromKeys(new NSString[]{new NSString("properties")});
                    string twitter_id = properties.ObjectForKey(new NSString("properties")).ValueForKey(new NSString("user_id")).ToString();
                    //works!!

                    {
                        Notification n1 = new Notification(LocalisationManager.GetString(OsuString.TwitterSuccess),
                                                        string.Format(LocalisationManager.GetString(OsuString.TwitterSuccessDetails), account.Username),
                                                        NotificationStyle.Okay,
                                                        null);
                        GameBase.Notify(n1);

                        GameBase.Config.SetValue<string>("username", account.Username);
                        GameBase.Config.SetValue<string>("hash", "ios-" + account.Identifier);
                        GameBase.Config.SetValue<string>("twitterId", twitter_id);
                        GameBase.Config.SaveConfig();

                        Director.ChangeMode(Director.CurrentOsuMode);
                    }
                });
            GameBase.Notify(n);
        }

#else
        private void HandleTwitterAuth(object sender, EventArgs args)
        {
            //not available on PC builds.
        }
#endif

        int lastEffectSound;
        private pButton buttonFingerGuides;
        private pButton buttonEasyMode;
        private pSprite s_Header;

        internal static void DisplayFingerGuideDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.UseFingerGuides), LocalisationManager.GetString(OsuString.UseGuideFingers_Explanation),
                        NotificationStyle.YesNo,
                        delegate(bool yes)
                        {
                            GameBase.Config.SetValue<bool>(@"GuideFingers", yes);

                            Options o = Director.CurrentMode as Options;
                            if (o != null) o.UpdateButtons();
                        });
            GameBase.Notify(notification);
        }

        private void UpdateButtons()
        {
            buttonEasyMode.SetStatus(GameBase.Config.GetValue<bool>(@"EasyMode", false));
            buttonFingerGuides.SetStatus(GameBase.Config.GetValue<bool>(@"GuideFingers", false));
        }

        internal static void DisplayEasyModeDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.DefaultToEasyMode), LocalisationManager.GetString(OsuString.DefaultToEasyMode_Explanation),
                        NotificationStyle.YesNo,
                        delegate(bool yes)
                        {
                            GameBase.Config.SetValue<bool>(@"EasyMode", yes);

                            Options o = Director.CurrentMode as Options;
                            if (o != null) o.UpdateButtons();
                        });
            GameBase.Notify(notification);
        }

        public override void Dispose()
        {
            ScrollPosition = smd.ScrollPosition;

            GameBase.Config.SetValue<int>("VolumeEffect", (int)(AudioEngine.Effect.Volume * 100));
            GameBase.Config.SetValue<int>("VolumeMusic", (int)(AudioEngine.Music.MaxVolume * 100));
            GameBase.Config.SaveConfig();

            topMostSpriteManager.Dispose();

            smd.Dispose();
            base.Dispose();
        }

        public override bool Draw()
        {
            base.Draw();
            smd.Draw();
            topMostSpriteManager.Draw();
            return true;
        }

        public override void Update()
        {
            s_Header.Position.Y = Math.Min(0, -smd.ScrollPercentage * 20);

            smd.Update();
            base.Update();
            topMostSpriteManager.Update();
        }
    }
}
