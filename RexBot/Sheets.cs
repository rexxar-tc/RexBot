using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Timer = System.Timers.Timer;

namespace RexBot
{
    public class Sheets
    {
        private readonly Dictionary<int, string> _reportStatuses = new Dictionary<int, string>();
        private SheetsService _service;
        private readonly string _sheetId;
        private readonly Timer _updateTimer;

        public Sheets(string sheetId, bool runTimer = false)
        {
            _sheetId = sheetId;
            Init();
            if (runTimer)
            {
                _updateTimer = new Timer(300000);
                _updateTimer.AutoReset = true;
                _updateTimer.Elapsed += (x, y) => Update().RunSynchronously();
                _updateTimer.Start();
            }
            Task.Run(Update);
        }

        private async Task Update()
        {
            //Console.WriteLine("Update");
            ValueRange statusReq = await _service.Spreadsheets.Values.Get(_sheetId, "Internal List!A2:G1000").ExecuteAsync();
            var channel = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetGuild(125011928711036928).GetChannel(136097351134740480);
            for (int i = 0; i < statusReq.Values.Count; i++)
            {
                IList<object> row = statusReq.Values[i];

                string status;
                string newStatus = row[4].ToString().Trim('\n', '\r', ' ');
                if (!_reportStatuses.TryGetValue(row[3].GetHashCode(), out status))
                {
                    _reportStatuses.Add(row[3].GetHashCode(), newStatus);
                    continue;
                }

                if (status == newStatus)
                    continue;

                string[] split = row[0].ToString().Split(':');
                string mention = $"<@{split[1]}>";
                string reportSummary;
                if (row[3].ToString().Length > 50)
                    reportSummary = row[3].ToString().Substring(0, 50) + "...";
                else
                    reportSummary = row[3].ToString();

                _reportStatuses[row[3].GetHashCode()] = newStatus;

                Console.WriteLine(newStatus);

                switch (newStatus)
                {
                    case "New Report":
                        break;
                    case "Cannot Reproduce":
                    case "Testing":
                        await channel.SendMessageAsync($"{mention} Your bug report ```{reportSummary}``` Has been updated to {newStatus}");
                        break;
                    case "Won't Do":
                    case "Resolved":
                        await channel.SendMessageAsync($"{mention} Your bug report ```{reportSummary}``` Has been updated to {newStatus}");
                        await DeleteRow("Internal List", i + 1);
                        await AppendRow("Old Reports", row.Cast<string>().ToArray());
                        await RexBotCore.Instance.PublicSheet.DeleteRow("Internal List", i + 1);
                        await RexBotCore.Instance.PublicSheet.AppendRow("Old Reports", new[]
                                                                                       {
                                                                                           row[2].ToString(),
                                                                                           row[3].ToString(),
                                                                                           row[4].ToString(),
                                                                                           row[5].ToString(),
                                                                                           row.ElementAtOrDefault(6)?.ToString() ?? "",
                                                                                       });
                        break;
                    case "Delete - No Response":
                        await DeleteRow("Internal List", i + 1);
                        await RexBotCore.Instance.PublicSheet.DeleteRow("Internal List", i + 1);
                        break;
                    case "Move to Jira":
                        var meta = new IssueMetadata {IsCTG = false, ReporterId = ulong.Parse(split[1]), ReporterName = RexBotCore.Instance.RexbotClient.GetUser(ulong.Parse(split[1])).Username};
                        await RexBotCore.Instance.Jira.AddIssue(JiraManager.ProjectKey.SE, reportSummary, row[3].ToString(), meta);
                        Console.WriteLine(reportSummary);
                        Console.WriteLine("Moved report to Jira");
                        await DeleteRow("Internal List", i + 1);
                        await AppendRow("Old Reports", row.Cast<string>().ToArray());
                        break;
                    default:
                        throw new Exception(newStatus);
                }
            }
            statusReq = await _service.Spreadsheets.Values.Get(_sheetId, "CTG Reports!A2:G1000").ExecuteAsync();
            /*var channelSE*/
            channel = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetGuild(125011928711036928).GetChannel(166886199200448512);
            //var channelME = (ISocketMessageChannel)RexBotCore.Instance.RexbotClient.GetGuild(125011928711036928).GetChannel(222685377201307648);
            for (int i = 0; i < statusReq.Values.Count; i++)
            {
                IList<object> row = statusReq.Values[i];
                //ulong channelId = ulong.Parse(row[1].ToString().Split(':')[1]);
                //if (channelId == 166886199200448512)
                //    channel = channelSE;
                //else if (channelId == 222685377201307648)
                //    channel = channelME;
                string status;
                string newStatus = row[4].ToString().Trim('\n', '\r', ' ');
                if (!_reportStatuses.TryGetValue(row[3].GetHashCode(), out status))
                {
                    _reportStatuses.Add(row[3].GetHashCode(), newStatus);
                    continue;
                }

                if (status == newStatus)
                    continue;

                string[] split = row[0].ToString().Split(':');
                string mention = $"<@{split[1]}>";
                string reportSummary;
                if (row[3].ToString().Length > 50)
                    reportSummary = row[3].ToString().Substring(0, 50) + "...";
                else
                    reportSummary = row[3].ToString();

                _reportStatuses[row[3].GetHashCode()] = newStatus;

                Console.WriteLine(newStatus);

                switch (newStatus)
                {
                    case "New Report":
                        break;
                    case "Cannot Reproduce":
                    case "Testing":
                        await channel.SendMessageAsync($"{mention} Your bug report ```{reportSummary}``` Has been updated to {newStatus}");
                        break;
                    case "Won't Do":
                    case "Resolved":
                        await channel.SendMessageAsync($"{mention} Your bug report ```{reportSummary}``` Has been updated to {newStatus}");
                        await DeleteRow("CTG Reports", i + 1);
                        await AppendRow("Old Reports", row.Cast<string>().ToArray());
                        break;
                    case "Delete - No Response":
                        await DeleteRow("CTG Reports", i + 1);
                        break;
                    case "Move to Jira":
                        var meta = new IssueMetadata {IsCTG = true, ReporterId = ulong.Parse(split[1]), ReporterName = RexBotCore.Instance.RexbotClient.GetUser(ulong.Parse(split[1])).Username};
                        await RexBotCore.Instance.Jira.AddIssue(JiraManager.ProjectKey.SE, reportSummary, row[3].ToString(), meta);
                        await DeleteRow("CTG Reports", i + 1);
                        await AppendRow("Old Reports", row.Cast<string>().ToArray());
                        Console.WriteLine(reportSummary);
                        Console.WriteLine("Moved to Jira");
                        break;
                    default:
                        throw new Exception(newStatus);
                }
            }
        }

        public void Init()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                bool exist = false;
                string credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-RexBot.json");
                if (File.Exists(credPath))
                {
                    exist = true;
                    Console.WriteLine("Loading saved credentials...");
                }
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] {SheetsService.Scope.Spreadsheets},
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                if (!exist)
                    Console.WriteLine("Credential file saved to: " + credPath);
            }
            _service = new SheetsService(new BaseClientService.Initializer
                                         {
                                             HttpClientInitializer = credential,
                                             ApplicationName = "RexBot",
                                         });
        }

        //why must you hurt me in this way, Google?
        public async Task<IList<IList<object>>> GetValues(string range)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_sheetId, range);
            ValueRange response = await request.ExecuteAsync();
            return response.Values;
        }

        public async Task<int> GetLastRow(string pageName)
        {
            int range = 1;
            do
            {
                SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_sheetId, $"Internal List!A{range}:A{range + 500}");
                ValueRange response = await request.ExecuteAsync();
                if (response.Values.Count < 100)
                    return response.Values.Count + range - 1;
                range += 100;
            }
            while (range < 2000);

            return -1;
        }

        public async Task<bool> AppendRow(string pageName, string[] data)
        {
            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(new ValueRange
                                                                                                            {
                                                                                                                Range = $"{pageName}!A2:G",
                                                                                                                MajorDimension = "ROWS",
                                                                                                                Values = new List<IList<object>> {new List<object>(data)}
                                                                                                            }, _sheetId, $"{pageName}!A2:G");

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> AppendRow(string pageName, IList<object> data)
        {
            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(new ValueRange
            {
                Range = $"{pageName}!A2:G",
                MajorDimension = "ROWS",
                Values = new List<IList<object>> { data }
            }, _sheetId, $"{pageName}!A2:G");

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> AppendRows(string pageName, IList<IList<object>> data)
        {
            SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(new ValueRange
            {
                Range = $"{pageName}!A2:G",
                MajorDimension = "ROWS",
                Values = data,
            }, _sheetId, $"{pageName}!A2:G");

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> IncrementCell(string cell)
        {
            IList<IList<object>> result = await GetValues(cell);

            if ((result == null) || !result.Any())
            {
                Console.WriteLine("Couldn't get cell");
                return false;
            }

            int value;

            if (!int.TryParse(result[0][0].ToString(), out value))
            {
                Console.WriteLine("Bad parsing");
                return false;
            }

            value++;


            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(new ValueRange
                                                                                                            {
                                                                                                                Range = cell,
                                                                                                                MajorDimension = "ROWS",
                                                                                                                Values = new List<IList<object>> {new List<object> {value}},
                                                                                                            }, _sheetId, cell);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> AppendCell(string cell, string data, bool createIfEmpty = true)
        {
            IList<IList<object>> result = await GetValues(cell);

            string value;

            if ((result == null) || !result.Any())
            {
                if (!createIfEmpty)
                {
                    Console.WriteLine("Couldn't get cell");
                    return false;
                }
                value = data;
            }
            else
            {
                value = result[0][0] + data;
            }

            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(new ValueRange
                                                                                                            {
                                                                                                                Range = cell,
                                                                                                                MajorDimension = "ROWS",
                                                                                                                Values = new List<IList<object>> {new List<object> {value}},
                                                                                                            }, _sheetId, cell);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> OverwriteCell(string cell, string data)
        {
            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(new ValueRange
            {
                Range = cell,
                MajorDimension = "ROWS",
                Values = new List<IList<object>> { new List<object> { data } },
            }, _sheetId, cell);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            await request.ExecuteAsync();

            return true;
        }

        public async Task<bool> DeleteRow(string pageName, int row)
        {
            SpreadsheetsResource.GetRequest pageReq = _service.Spreadsheets.Get(_sheetId);
            Spreadsheet resp = await pageReq.ExecuteAsync();
            Sheet sheet = resp.Sheets.FirstOrDefault(s => s.Properties.Title.Equals(pageName, StringComparison.CurrentCultureIgnoreCase));
            if (sheet == null)
                return false;

            var dimReq = new DeleteDimensionRequest
                         {
                             Range = new DimensionRange
                                     {
                                         Dimension = "ROWS",
                                         StartIndex = row,
                                         EndIndex = row + 1,
                                         SheetId = sheet.Properties.SheetId,
                                     }
                         };

            SpreadsheetsResource.BatchUpdateRequest request = _service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
                                                                                                {
                                                                                                    Requests = new List<Request>
                                                                                                               {
                                                                                                                   new Request
                                                                                                                   {
                                                                                                                       DeleteDimension = dimReq
                                                                                                                   }
                                                                                                               },
                                                                                                }, _sheetId);
            await request.ExecuteAsync();
            return true;
        }

        public async Task<bool> EmptyPage(string pageName)
        {
            SpreadsheetsResource.GetRequest pageReq = _service.Spreadsheets.Get(_sheetId);
            Spreadsheet resp = await pageReq.ExecuteAsync();
            Sheet sheet = resp.Sheets.FirstOrDefault(s => s.Properties.Title.Equals(pageName, StringComparison.CurrentCultureIgnoreCase));
            //int r = await GetLastRow(pageName);
            if (sheet == null)
                return false;

            var dimReq = new DeleteDimensionRequest
            {
                Range = new DimensionRange
                {
                    Dimension = "ROWS",
                    StartIndex = 1,
                    EndIndex = 1000,
                    SheetId = sheet.Properties.SheetId,
                }
            };

            SpreadsheetsResource.BatchUpdateRequest request = _service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                                                                                                               {
                                                                                                                   new Request
                                                                                                                   {
                                                                                                                       DeleteDimension = dimReq
                                                                                                                   }
                                                                                                               },
            }, _sheetId);
            await request.ExecuteAsync();
            return true;
        }

        public async Task<int> FindRow(string pageName, string columnAValue)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_sheetId, $"'{pageName}'!A:A");
            var response = await request.ExecuteAsync();
            if (response == null)
                return -1;

            for (int i = 0; i < response.Values.Count; i++)
            {
                if ((response.Values[i][0] as string) == columnAValue)
                    return i;
            }

            return -1;
        }

        public async Task MoveRow(string pageFrom, string pageTo, int indexFrom)
        {
            var values = await GetValues($"{pageFrom}!A{indexFrom}:F{indexFrom}");
            await DeleteRow(pageFrom, indexFrom);
            await AppendRow(pageTo, values[0]);
        }
    }
}