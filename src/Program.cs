using System.IO;
using dnlib.DotNet;
using System.Collections.Generic;
using System;
using dnlib.DotNet.Writer;

namespace AssemblyTools
{
    class Program
    {
        private static string moddedAssembly = @"E:\Steam\steamapps\common\Snowtopia\Snowtopia_Data\Managed\Assembly-CSharp.dll";
        private static string originalAssembly = @"E:\dev\Modding\Snowtopia\0.14.12\Assembly-CSharp-CLEAN.dll";
        private static string outputASMLocation1 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\FullAssemblyTest\output.dll";
        private static string outputLocation1 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\FullAssemblyTest\Out\";
        private static string outputLocation2 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\FullAssemblyTest\OutNew\";

        /*private static string moddedAssembly = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\PCEtest\PrecompiledExtensions-NEW.dll";
        private static string originalAssembly = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\PCEtest\PrecompiledExtensions-OLD.dll";
        private static string outputLocation1 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\PCEtest\Out\";
        private static string outputLocation2 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\PCEtest\OutNew\";
        private static string outputASMLocation1 = @"E:\dev\Modding\Snowtopia\Compare-Output\Test\PCEtest\PrecompiledExtensions-OUT.dll";*/

        static void Main(string[] args)
        {
            ModuleContext modCtx = ModuleDef.CreateModuleContext();
            ModuleDefMD moddedModule = ModuleDefMD.Load(moddedAssembly, modCtx);
            ModuleDefMD originalModule = ModuleDefMD.Load(originalAssembly, modCtx);

            //Utils.PrintOverview(moddedModule, originalModule);
            Utils.OutputAll(outputLocation1, originalModule, moddedModule);

            ModuleDefMD outputModule = ModuleDefMD.Load(originalAssembly, modCtx);
            SaveUtils.ApplySave(outputModule, outputLocation1 + @"\save.json");

            //MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;

            Utils.OutputAll(outputLocation2, outputModule, moddedModule);
            outputModule.Write(outputASMLocation1);

            //Utils.PrintMethodDiffs(moddedModule, outputModule, "LiftTowerCablePathFetcher", "GetCablePath");

            foreach(TypeDef type in moddedModule.Types)
            {
                //Console.WriteLine(type.FullName);
            }

            /*ModuleContext modCtx = ModuleDef.CreateModuleContext();
            ModuleDefMD moddedModule = ModuleDefMD.Load(outputASMLocation1, modCtx);
            ModuleDefMD originalModule = ModuleDefMD.Load(originalAssembly, modCtx);

            Utils.OutputAll(outputLocation2, originalModule, moddedModule);*/
        }
    }
}