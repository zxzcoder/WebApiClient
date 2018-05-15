﻿using Mono.Cecil;
using System;
using System.IO;
using System.Linq;

namespace WebApiClient.AOT.Task
{
    /// <summary>
    /// 表示程序集
    /// </summary>
    class CeAssembly : IDisposable
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly Action<string> logger;

        /// <summary>
        /// 程序集
        /// </summary>
        private readonly AssemblyDefinition assembly;

        /// <summary>
        /// 程序集
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <param name="searchDirectories">依赖项搜索目录</param>
        /// <param name="logger">日志</param>
        /// <exception cref="FileNotFoundException"></exception>
        public CeAssembly(string fileName, string[] searchDirectories, Action<string> logger)
        {
            if (File.Exists(fileName) == false)
            {
                throw new FileNotFoundException("找不到文件", fileName);
            }

            var resolver = new DefaultAssemblyResolver();
            foreach (var dir in searchDirectories)
            {
                logger($"添加搜索目录-> {dir}");
                resolver.AddSearchDirectory(dir);
            }

            var parameter = new ReaderParameters
            {
                ReadWrite = true,
                ReadSymbols = true,
                AssemblyResolver = resolver
            };

            this.logger = logger;
            this.assembly = AssemblyDefinition.ReadAssembly(fileName, parameter);
        }

        /// <summary>
        /// 写入代理类型
        /// </summary>
        /// <returns></returns>
        public bool WirteProxyTypes()
        {
            var httpApiInterfaces = this.assembly
                .MainModule
                .GetTypes()
                .Select(item => new CeInterface(item))
                .Where(item => item.IsHttpApiInterface())
                .ToArray();

            var willSave = false;
            foreach (var @interface in httpApiInterfaces)
            {
                var proxyType = new CeProxyType(@interface);
                if (proxyType.IsDefinded() == false)
                {
                    this.logger($"正在写入IL-> {@interface.Type.FullName}");
                    this.assembly.MainModule.Types.Add(proxyType.Build());
                    willSave = true;
                }
            }

            if (willSave == true)
            {
                var parameters = new WriterParameters
                {
                    WriteSymbols = true
                };
                this.assembly.Write(parameters);
            }
            return willSave;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.assembly.Dispose();
        }
    }
}