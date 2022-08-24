using HinaBot_NeoAspect.Config;
using HinaBot_NeoAspect.DataStructures;
using HinaBot_NeoAspect.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HinaBot_NeoAspect.Commands
{
    public class ReplyCommand : ICommand
    {
        public List<string> Alias => new List<string>
        {
            "/reply"
        };

        public async Task Run(CommandArgs args)
        {
            string[] splits = args.Arg.Trim().Split(' ');
            if (string.IsNullOrWhiteSpace(args.Arg.Trim()))
            {
                await args.Callback("/reply <add/del/list>[1234] ...\n1���ؼ��ʱȽ�\n2��������ʽ\n3��ȫ��������ʽ\n4��C#�ű�");
                return;
            }

            var config = Configuration.GetConfig<ReplyHandler>();
            var qq = args.Source.FromQQ;

            switch (splits[0])
            {
                case "reload":
                    if (await args.Source.HasPermission("reply.reload", -1))
                    {
                        config.Load();
                        await args.Callback("�����ļ����سɹ�");
                    }
                    break;
                case "save":
                    if (await args.Source.HasPermission("reply.save", -1))
                    {
                        config.Save();
                        await args.Callback("�����ļ�����ɹ�");
                    }
                    break;
                case "add2":
                case "add3":
                case "add4":
                    {
                        if (splits.Length < 3)
                        {
                            await args.Callback("������������Ŷ");
                            return;
                        }
                        Regex reg;

                        try
                        {
                            reg = new Regex($"^{Utils.FixRegex(splits[1])}$", RegexOptions.Multiline | RegexOptions.Compiled);
                        }
                        catch
                        {
                            await args.Callback("�ղ���һ�۾Ϳ�����������ʽ������Ŷ");
                            return;
                        }

                        var reply = new Reply
                        {
                            qq = qq,
                            reply = string.Concat(splits.Skip(2).Select((s) => s + ' ')).Trim()
                        };

                        var data = config[int.Parse(splits[0].Substring(3))];

                        if (splits[0] == "add4" && !await args.Source.HasPermission("reply.add4", -1))
                        {
                            await args.Callback("Ȩ�޲��㣡");
                            return;
                        }

                        if (data.TryGetValue(splits[1], out var t))
                        {
                            if (t.Any(r => r.reply == reply.reply))
                            {
                                await args.Callback($"`{splits[1]}` => `{reply.reply}` �Ѵ��ڣ�");
                                return;
                            }
                            t.Add(reply);
                        }
                        else
                        {
                            data.Add(splits[1], new List<Reply> { reply });
                            ReplyHandler.regexCache[splits[1]] = reg;
                        }


                        if (splits[0] == "add4")
                        {
                            try
                            {
                                await config.ReloadAssembly();
                            }
                            catch (Exception e)
                            {
                                await args.Callback(e.ToString());
                                try
                                {
                                    config.Load();
                                }
                                catch
                                {
                                    config.LoadDefault();
                                }
                                break;
                            }
                        }

                        config.Save();
                        await args.Callback($"��ӳɹ�{(splits[0] == "add" ? "" : "������")}��\n`{splits[1]}` => `{reply.reply}`");

                        break;
                    }
                case "del2":
                case "del3":
                case "del4":
                    {
                        if (splits.Length < 3)
                        {
                            await args.Callback("������������Ŷ");
                            return;
                        }

                        var data = config[int.Parse(splits[0].Substring(3))];
                        var result = Utils.TryGetValueStart(data, (pair) => pair.Key, splits[1], out var list);
                        var replystart = string.Concat(splits.Skip(2).Select((s) => s + ' ')).Trim();

                        if (string.IsNullOrEmpty(result))
                        {
                            var result2 = Utils.TryGetValueStart(list.Value, (reply) => reply.reply, replystart, out var reply);

                            if (string.IsNullOrEmpty(result2))
                            {
                                if (reply.qq == qq || await args.Source.HasPermission("reply.deloverride", -1))
                                {
                                    list.Value.Remove(reply);
                                    if (list.Value.Count == 0)
                                        data.Remove(list.Key);
                                    config.Save();
                                    await args.Callback($"�ɹ��Ƴ���`{list.Key}` => `{reply.reply}`");
                                }
                                else
                                    await args.Callback("");
                            }
                            else
                                await args.Callback(result2);
                        }
                        else
                            await args.Callback(result);

                        break;
                    }
                case "list2":
                case "list3":
                case "list4":
                    {
                        var data = config[int.Parse(splits[0].Substring(4))];
                        if (splits.Length == 1)
                        {
                            if (!await args.Source.HasPermission("reply.list", -1))
                            {
                                await args.Callback("Ȩ�޲��㣡");
                                return;
                            }
                            await args.Callback("���п��õĻظ���\n" + string.Concat(data.Select((pair) => pair.Key + "\n")));
                        }
                        else if (splits.Length == 2)
                        {
                            var result = Utils.TryGetValueStart(data, (pair) => pair.Key, splits[1], out var list);

                            if (string.IsNullOrEmpty(result))
                                await args.Callback($"�ؼ���`{list.Key}`�����лظ���\n{string.Concat(list.Value.Select((reply) => $"`{reply.reply}` (by {reply.qq})\n"))}");
                            else
                                await args.Callback(result);

                        }
                        else
                            await args.Callback("������������Ŷ");
                        break;
                    }
                case "search2":
                case "search3":
                case "search4":
                    {
                        var data = config[int.Parse(splits[0].Substring(6))];

                        if (splits.Length == 1) return;

                        var result = ReplyHandler.FitRegex(data, splits[1]);
                        await args.Callback($"ƥ������`{splits[1]}`�Ļظ���\n" +
                            $"{string.Join('\n', data.Where(tuple => ReplyHandler.regexCache[tuple.Key].Match(splits[1]).Success).Select(tuple => tuple.Key).Select(str => str[1..^1]))}");
                        break;
                    }
            }
        }
    }
}
