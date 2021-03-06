﻿using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace WebAutomationFramework
{
    public sealed class Driver
    {
        private static readonly int MAX_WEB_DRIVERS = 2;  // Max web drivers to run in parallel. NUnit uses 1 thread per core.
        private static readonly int GET_WEB_DRIVER_POLL_TIME = 1000;
        private static readonly Dictionary<int, IWebDriver> webDrivers = new Dictionary<int, IWebDriver>();  // Key = thread ID. Value = web driver instance.
        private static readonly Object ThreadLock = new Object();  // To sync `Driver` class.

        // Make `Driver` as singleton.
        private static volatile Driver instance;
        private Driver() { }
        public static Driver Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (ThreadLock)
                    {
                        if (instance == null)
                        {
                            instance = new Driver();
                        }
                    }
                }
                return instance;
            }
        }

        /*
         * Return a new web driver for current thread or an existing one if it was previously created.
         */
        public IWebDriver GetWebDriver(string driverName = "Chrome")
        {
            IWebDriver currentWebDriver = null;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                lock (ThreadLock)
                {
                    if (webDrivers.TryGetValue(threadId, out currentWebDriver) == false)
                    {
                        if (webDrivers.Count < MAX_WEB_DRIVERS)
                        {
                            currentWebDriver = StartWebDriver(driverName);
                            webDrivers[threadId] = currentWebDriver;
                        }
                    }
                }

                if (currentWebDriver != null)
                {
                    return currentWebDriver;
                }

                Thread.Sleep(GET_WEB_DRIVER_POLL_TIME);
            }
        }

        private IWebDriver StartWebDriver(string driverName)
        {
            switch (driverName)
            {
                case "Chrome":
                    return new ChromeDriver();
                case "Firefox":
                    return new FirefoxDriver();
                default:
                    throw new Exception($"Unknown browser! Got: {driverName}.");
            }
        }

        public void StopWebDriver()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (ThreadLock)
            {
                if (webDrivers.TryGetValue(threadId, out IWebDriver currentWebDriver) == true)
                {
                    currentWebDriver.Quit();
                    webDrivers.Remove(threadId);
                }
            }
        }

        ~Driver()
        {
            // Close all web drivers if any.
            IWebDriver currentWebDriver;
            foreach (KeyValuePair<int, IWebDriver> entry in webDrivers)
            {
                currentWebDriver = entry.Value;
                currentWebDriver.Quit();
            }
        }
    }
}
