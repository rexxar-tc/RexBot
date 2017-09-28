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

        public Sheets(string sheetId)
        {
            _sheetId = sheetId;
            Init();
            //Task.Run(Update);
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
            try
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

                var insReq = new InsertDimensionRequest()
                             {
                                 Range = new DimensionRange()
                                         {
                                             Dimension = "ROWS",
                                             StartIndex = 1,
                                             EndIndex = 2,
                                             SheetId = sheet.Properties.SheetId,
                                         }
                             };

                SpreadsheetsResource.BatchUpdateRequest request = _service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
                                                                                                    {
                                                                                                        Requests = new List<Request>
                                                                                                                   {
                                                                                                                       new Request
                                                                                                                       {
                                                                                                                           DeleteDimension = dimReq,
                                                                                                                       }
                                                                                                                   },
                                                                                                    }, _sheetId);
                await request.ExecuteAsync();

                var app = _service.Spreadsheets.Values.Append(new ValueRange
                {
                    Range = $"{pageName}!A2:G",
                    MajorDimension = "ROWS",
                    Values = new List<IList<object>> { new List<object>() {string.Empty} }
                }, _sheetId, $"{pageName}!A2:G");

                app.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                await app.ExecuteAsync();
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
                return false;
            }
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