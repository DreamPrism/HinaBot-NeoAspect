using HinaBot_NeoAspect.Commands;
using HinaBot_NeoAspect.Config;
using HinaBot_NeoAspect.Handler;
using HinaBot_NeoAspect.Services;
using Sora.Entities;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.EventArgs.WebsocketEvent;
using Sora.Net;
using Sora.OnebotModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect
{
    class Program
    {
        static void LoadAll()
        {
            Configuration.Register<Whitelist>();
            Configuration.Register<Blacklist>();
            Configuration.Register<BlacklistF>();
            Configuration.Register<Save>();
            Configuration.Register<PermissionConfig>();
            Configuration.Register<Motd>();
            Configuration.Register<PokeReply>();
            Configuration.Register<AssemblyUsingsConfig>();
            Configuration.Register<LocalDataConfiguration>();
            Configuration.Register<ReplyHandler>();
            MessageHandler.Register(Configuration.GetConfig<ReplyHandler>());
            MessageHandler.Register<WhitelistHandler>();
            MessageHandler.Register<WhitelistCommand>();
            MessageHandler.Register<BlacklistCommand>();
            MessageHandler.Register<PermCommand>();
            MessageHandler.Register<ReplyCommand>();
            MessageHandler.Register<MotdCommand>();
            MessageHandler.Register<PokeReplyCommand>();

            if (!Directory.Exists(Configuration.ConfigPath)) Directory.CreateDirectory(Configuration.ConfigPath);
            Configuration.LoadAll();

            if (!Directory.Exists("imagecache")) Directory.CreateDirectory("imagecache");

            Generator.Instance = new Generator("Generator");
        }
        static async Task Main(string[] args)
        {
            LoadAll();
            args = new string[] { "", "8000" };
            uint port = uint.Parse(args[1]);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            var service = SoraServiceFactory.CreateInstance(new ClientConfig()
            {
                Host = "127.0.0.1",
                Port = uint.Parse(args[1])
            });

            service.Event.OnClientConnect += Event_OnClientConnect;
            service.Event.OnFriendRequest += Event_OnFriendRequest;
            service.Event.OnGroupMessage += Event_OnGroupMessage;
            //由于私信功能封号风险极大，因此停用了私信回复
            //service.Event.OnPrivateMessage += Event_OnPrivateMessage;
            service.Event.OnGroupMemberChange += Event_OnGroupMemberChange;
            //群戳一戳，感觉没啥必要所以就没写相关的代码
            service.Event.OnGroupPoke += Event_OnGroupPoke;
            Console.WriteLine("connected to server");
            await service.StartService();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }
        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var ex = e.Exception;
            while (ex is AggregateException e2) ex = e2.InnerException;
            if (ex is IOException) return;
            Console.WriteLine(e.Exception);
        }
        private static async ValueTask Event_OnGroupPoke(string type, GroupPokeEventArgs eventArgs)
        {
        }

        private static async ValueTask Event_OnGroupMemberChange(string type, GroupMemberChangeEventArgs eventArgs)
        {
            var session = eventArgs.SoraApi;
            var group = eventArgs.SourceGroup.Id;
            if (eventArgs.SubType >= MemberChangeType.Approve && Configuration.GetConfig<Whitelist>().hash.Contains(group))
            {
                var motd = Configuration.GetConfig<Motd>()[group];
                if (motd != null)
                {
                    motd = motd.Replace("{AtNew}", $"[mirai:at={eventArgs.ChangedUser.Id}] ");
                    await session.SendGroupMessage(group, Utils.GetMessageChain(motd));
                }
            }
        }
        private static async ValueTask Event_OnPrivateMessage(string type, PrivateMessageEventArgs eventArgs)
        {
            if (!Source.AdminQQs.Contains(eventArgs.SenderInfo.UserId)) return;
            await MessageHandler.OnMessage(eventArgs.SoraApi, Utils.GetCQMessage(eventArgs.Message), new Source
            {
                Session = eventArgs.SoraApi,
                FromGroup = 0,
                FromQQ = eventArgs.SenderInfo.UserId
            });
        }

        private static async ValueTask Event_OnGroupMessage(string type, GroupMessageEventArgs eventArgs)
        {
            await MessageHandler.OnMessage(eventArgs.SoraApi, Utils.GetCQMessage(eventArgs.Message), new Source
            {
                Session = eventArgs.SoraApi,
                FromGroup = eventArgs.SourceGroup.Id,
                FromQQ = eventArgs.SenderInfo.UserId
            });
        }

        private static async ValueTask Event_OnFriendRequest(string type, FriendRequestEventArgs eventArgs)
        {
            await eventArgs.SoraApi.SetFriendAddRequest(eventArgs.RequsetFlag, false);
        }

#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        private static async ValueTask Event_OnClientConnect(string type, ConnectEventArgs e)
        {
            MessageHandler.session = e.SoraApi;
            MessageHandler.booted = true;
        }
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
    }
}
