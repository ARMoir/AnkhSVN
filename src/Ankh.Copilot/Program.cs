// Copyright 2026 The AnkhSVN Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using GitHub.Copilot;

namespace Ankh.Copilot
{
    static class Program
    {
        static int Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                return MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        static async Task<int> MainAsync()
        {
            string prompt = await Console.In.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.Error.WriteLine("No commit-message prompt was supplied.");
                return 2;
            }

            CopilotClient client = new CopilotClient();
            try
            {
                await client.StartAsync();

                CopilotSession session = await client.CreateSessionAsync(
                    new SessionConfig
                    {
                        AvailableTools = new List<string>()
                    });

                try
                {
                    AssistantMessageEvent response = await session.SendAndWaitAsync(
                        new MessageOptions { Prompt = prompt },
                        TimeSpan.FromSeconds(75));

                    string message = response != null && response.Data != null
                        ? response.Data.Content
                        : null;

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        Console.Error.WriteLine("GitHub Copilot returned no commit message.");
                        return 3;
                    }

                    Console.Out.Write(message);
                    return 0;
                }
                finally
                {
                    await session.DisposeAsync();
                }
            }
            finally
            {
                await client.DisposeAsync();
            }
        }
    }
}
