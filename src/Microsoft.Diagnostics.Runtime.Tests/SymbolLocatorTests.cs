﻿using System;
using Xunit;
using Microsoft.Diagnostics.Runtime.Utilities;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Utilities.Pdb;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class SymbolLocatorTests
    {
        static public readonly string WellKnownDac = "mscordacwks_X86_X86_4.6.96.00.dll";
        static public readonly int WellKnownDacTimeStamp = 0x55b96946;
        static public readonly int WellKnownDacImageSize = 0x006a8000;

        static public readonly string WellKnownNativePdb = "clr.pdb";
        static public readonly Guid WellKnownNativePdbGuid = new Guid("0350aa66-2d49-4425-ab28-9b43a749638d");
        static public readonly int WellKnownNativePdbAge = 2;

        static internal SymbolLocator GetLocator()
        {
            return new DefaultSymbolLocator() { SymbolCache = Helpers.TestWorkingDirectory };
        }
        

        [Fact]
        public void SymbolLocatorTimeoutTest()
        {
            var locator = GetLocator();
            locator.Timeout = 10000;
            locator.SymbolCache += "\\TestTimeout";

            string pdb = locator.FindPdb(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge);
            Assert.NotNull(pdb);
        }

        [Fact]
        public void FindBinaryNegativeTest()
        {
            SymbolLocator _locator = GetLocator();
            string dac = _locator.FindBinary(WellKnownDac, WellKnownDacTimeStamp + 1, WellKnownDacImageSize + 1, false);
            Assert.Null(dac);
        }

        [Fact]
        public void FindPdbNegativeTest()
        {
            SymbolLocator _locator = GetLocator();
            string pdb = _locator.FindPdb(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge + 1);
            Assert.Null(pdb);
        }
        [Fact]
        public async Task FindBinaryAsyncNegativeTest()
        {
            SymbolLocator _locator = GetLocator();

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindBinaryAsync(WellKnownDac, WellKnownDacTimeStamp + 1, WellKnownDacImageSize + 1, false));
            
            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string dac = await task;
                Assert.Null(dac);
            }
        }

        [Fact]
        public async Task FindPdbAsyncNegativeTest()
        {
            SymbolLocator _locator = GetLocator();

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindPdbAsync(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge + 1));
            
            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string pdb = await task;
                Assert.Null(pdb);
            }
        }

        [Fact]
        public void FindBinaryTest()
        {
            SymbolLocator _locator = GetLocator();
            string dac = _locator.FindBinary(WellKnownDac, WellKnownDacTimeStamp, WellKnownDacImageSize, false);
            Assert.NotNull(dac);
            Assert.True(File.Exists(dac));
        }

        [Fact]
        public void FindPdbTest()
        {
            SymbolLocator _locator = GetLocator();
            string pdb = _locator.FindPdb(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge);
            Assert.NotNull(pdb);
            Assert.True(File.Exists(pdb));

            Assert.True(PdbMatches(pdb, WellKnownNativePdbGuid, WellKnownNativePdbAge));
        }

        static bool PdbMatches(string pdb, Guid guid, int age)
        {
            PdbReader.GetPdbProperties(pdb, out Guid fileGuid, out int fileAge);

            return guid == fileGuid;
        }

        [Fact]
        public async Task FindBinaryAsyncTest()
        {
            SymbolLocator _locator = GetLocator();
            Task<string> first = _locator.FindBinaryAsync(WellKnownDac, WellKnownDacTimeStamp, WellKnownDacImageSize, false);

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindBinaryAsync(WellKnownDac, WellKnownDacTimeStamp, WellKnownDacImageSize, false));

            string dac = await first;

            Assert.NotNull(dac);
            Assert.True(File.Exists(dac));
            new PEFile(dac).Dispose();  // This will throw if the image is invalid.

            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string taskDac = await task;
                Assert.Equal(dac, taskDac);
            }
        }


        [Fact]
        public async Task FindPdbAsyncTest()
        {
            SymbolLocator _locator = GetLocator();
            Task<string> first = _locator.FindPdbAsync(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge);

            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
                tasks.Add(_locator.FindPdbAsync(WellKnownNativePdb, WellKnownNativePdbGuid, WellKnownNativePdbAge));

            string pdb = await first;
            
            Assert.NotNull(pdb);
            Assert.True(File.Exists(pdb));
            Assert.True(PdbMatches(pdb, WellKnownNativePdbGuid, WellKnownNativePdbAge));

            // Ensure we got the same answer for everything.
            foreach (var task in tasks)
            {
                string taskPdb = await task;
                Assert.Equal(taskPdb, pdb);
            }
        }
    }
}
