using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using DiffMatchPatch;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace AssemblyTools
{
    public class Utils
    {
        #region moduleManagement

        public static ModuleDef GetModule(string Module)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region masterMethods

        public static void OutputAll(string outputLocation, ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            var temp = Directory.CreateDirectory(outputLocation);
            var temp2 = outputLocation.Split(@"\");
            Save(temp, moddedModule, originalModule, temp2[temp2.Length-1]);
            OutputFullDetails(temp, moddedModule, originalModule);
        }

        public static void Save(DirectoryInfo directory, ModuleDefMD moddedModule, ModuleDefMD originalModule, string name)
        {
            Console.WriteLine("Saving");
            byte[] temp;

            FileStream saveFile = File.Create(directory.FullName + @"\save.json");
            Console.WriteLine("Getting Save");
            Save save = SaveUtils.GetSave(moddedModule, originalModule, "Save");
            Console.WriteLine("Got Save");
            Console.WriteLine("Serializing");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            string json = JsonConvert.SerializeObject(save, Formatting.Indented, settings);//Long boi
            Console.WriteLine("Serialized");
            temp = new UTF8Encoding(true).GetBytes(json);
            saveFile.Write(temp, 0, temp.Length);
            saveFile.Close();
            Console.WriteLine("Saved");
        }

        public static void OutputFullDetails(DirectoryInfo directory, ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            byte[] temp;

            Console.WriteLine("Writing overviews.");
            FileStream overviewFile = File.Create(directory.FullName + @"\overview.txt");
            temp = new UTF8Encoding(true).GetBytes(GetOverviewString(moddedModule, originalModule));
            overviewFile.Write(temp, 0, temp.Length);
            overviewFile.Close();

            FileStream operandTypesFile = File.Create(directory.FullName + @"\operand-types.txt");
            List<Type> instructionsUsed = GetOperandTypes(GetOperandTypes(originalModule), moddedModule);
            string usedTypes = "OPERAND-TYPES:\n";
            foreach (Type instructionType in instructionsUsed)
            {
                usedTypes += "\t" + instructionType.FullName + "\n";
            }
            temp = new UTF8Encoding(true).GetBytes(usedTypes);
            operandTypesFile.Write(temp, 0, temp.Length);
            operandTypesFile.Close();
            Console.WriteLine("Written overviews.");

            Console.WriteLine("Assembling changes.");
            var addedTypes = GetAddedTypes(moddedModule, originalModule);
            var removedTypes = GetAddedTypes(originalModule, moddedModule);
            var correspondingTypes = GetCorrespondingTypes(moddedModule, originalModule);
            Console.WriteLine("Assembled changes.");

            Console.WriteLine("Writing types addition/removal");
            //Removed types - simple, no detail needed
            string removedTypesString = "REMOVED-TYPES:";
            foreach(TypeDef removedType in removedTypes)
            {
                removedTypesString += "\t" + removedType.FullName+"\n";
            }
            FileStream removedTypesFile = File.Create(directory.FullName + @"\removed-types.txt");
            temp = new UTF8Encoding(true).GetBytes(removedTypesString);
            removedTypesFile.Write(temp, 0, temp.Length);
            removedTypesFile.Close();

            foreach (TypeDef addedType in addedTypes)
            {
                OutputFullTypeDetails(Directory.CreateDirectory(directory.FullName + @"\added-types\" + MakeFileSystemValidType(addedType.FullName)), addedType);
            }
            Console.WriteLine("Written types addition/removal");

            Console.WriteLine("Writing modified type data.");
            DirectoryInfo modifiedDirectory = Directory.CreateDirectory(directory.FullName + @"\modified-types");
            foreach (var type in correspondingTypes)
            {
                var moddedMethods = GetModdedMethods(type.Item1, type.Item2);
                if (moddedMethods.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    DirectoryInfo moddedMethodsDirectory = Directory.CreateDirectory(typeDirectory.FullName + @"\modified-methods");
                    foreach (var method in moddedMethods)
                    {                        
                        FileStream moddedMethodFile = File.Create(moddedMethodsDirectory.FullName + @"\" + MakeFileSystemValidMethod(method.Item1.FullName) + ".txt");
                        temp = new UTF8Encoding(true).GetBytes(GetDifferencesString(GetMethodDiffs(method.Item1, method.Item2)));
                        moddedMethodFile.Write(temp, 0, temp.Length);
                        moddedMethodFile.Close();
                    }
                }

                var addedMethods = GetAddedMethods(type.Item1, type.Item2);
                if (addedMethods.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    DirectoryInfo addedMethodsDirectory = Directory.CreateDirectory(typeDirectory.FullName + @"\added-methods");
                    foreach (var method in addedMethods)
                    {
                        FileStream addedMethodFile = File.Create(addedMethodsDirectory.FullName + @"\" + MakeFileSystemValidMethod(method.FullName) + ".txt");
                        temp = new UTF8Encoding(true).GetBytes(GetMethodString(method));
                        addedMethodFile.Write(temp, 0, temp.Length);
                        addedMethodFile.Close();
                    }
                }

                var removedMethods = GetAddedMethods(type.Item2, type.Item1);
                if (removedMethods.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    FileStream removedMethodsFile = File.Create(typeDirectory.FullName + @"\removed-methods.txt");
                    string removedMethodString = "REMOVED-METHODS:\n";
                    foreach(MethodDef method in removedMethods)
                    {
                        removedMethodString += "\t" + method.FullName + "\n";
                    }
                    temp = new UTF8Encoding(true).GetBytes(removedMethodString);
                    removedMethodsFile.Write(temp, 0, temp.Length);
                    removedMethodsFile.Close();
                }


                var addedFields = GetAddedFields(type.Item1, type.Item2);
                if (addedFields.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    FileStream addedFieldsFile = File.Create(typeDirectory.FullName + @"\added-fields.txt");
                    string addedFieldsString = "ADDED-FIELDS:\n";
                    foreach (FieldDef fieldDef in addedFields)
                    {
                        addedFieldsString += "\t";
                        addedFieldsString += GetModifiers(fieldDef.Attributes);
                        addedFieldsString += fieldDef.FieldType.TypeName;
                        addedFieldsString += " ";
                        addedFieldsString += fieldDef.Name;
                        addedFieldsString += " = ";
                        addedFieldsString += GetHex(fieldDef.InitialValue);
                        addedFieldsString += "\n";
                    }
                    temp = new UTF8Encoding(true).GetBytes(addedFieldsString);
                    addedFieldsFile.Write(temp, 0, temp.Length);
                    addedFieldsFile.Close();
                }

                var moddedFields = GetModdedFields(type.Item1, type.Item2);
                if (moddedFields.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    FileStream moddedFieldsFile = File.Create(typeDirectory.FullName + @"\modded-fields.txt");
                    string moddedFieldsString = "MODIFIED-FIELDS:\n";
                    foreach (FieldDef fieldDef in moddedFields)
                    {
                        moddedFieldsString += "\t";
                        moddedFieldsString += GetModifiers(fieldDef.Attributes);
                        moddedFieldsString += fieldDef.FieldType.TypeName;
                        moddedFieldsString += " ";
                        moddedFieldsString += fieldDef.Name;
                        moddedFieldsString += " = ";
                        moddedFieldsString += GetHex(fieldDef.InitialValue);
                        moddedFieldsString += "\n";
                    }
                    temp = new UTF8Encoding(true).GetBytes(moddedFieldsString);
                    moddedFieldsFile.Write(temp, 0, temp.Length);
                    moddedFieldsFile.Close();
                }

                var removedFields = GetAddedFields(type.Item2, type.Item1);
                if (removedFields.Count != 0)
                {
                    DirectoryInfo typeDirectory = Directory.CreateDirectory(modifiedDirectory.FullName + @"\" + MakeFileSystemValidType(type.Item1.FullName));
                    FileStream removedFieldsFile = File.Create(typeDirectory.FullName + @"\removed-fields.txt");
                    string removedFieldsString = "REMOVED-FIELDS:\n";
                    foreach (FieldDef fieldDef in removedFields)
                    {
                        removedFieldsString += "\t" + fieldDef.FullName + "\n";
                    }
                    temp = new UTF8Encoding(true).GetBytes(removedFieldsString);
                    removedFieldsFile.Write(temp, 0, temp.Length);
                    removedFieldsFile.Close();
                }
            }
            Console.WriteLine("Written modified type data.");
        }

        public static List<Type> GetOperandTypes(List<Type> previous, ModuleDef module)
        {
            foreach(TypeDef type in module.Types)
            {
                if (type == null || type.Methods == null)
                {
                    continue;
                }
                foreach (MethodDef method in type.Methods)
                {
                    if (method == null || method.Body == null || method.Body.Instructions == null)
                    {
                        continue;
                    }
                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        if(instruction == null || instruction.Operand == null)
                        {
                            continue;
                        }
                        Type operandType = instruction.Operand.GetType();
                        if (!previous.Contains(operandType))
                        {
                            previous.Add(operandType);
                        }
                    }
                }
            }
            return previous;
        }

        public static string GetOperandTypesStringDetailed(ModuleDef module)
        {
            string toReturn = "";
            List<Type> types = new List<Type>();
            foreach (TypeDef type in module.Types)
            {
                if (type == null || type.Methods == null)
                {
                    continue;
                }
                foreach (MethodDef method in type.Methods)
                {
                    if (method == null || method.Body == null || method.Body.Instructions == null)
                    {
                        continue;
                    }
                    foreach (Instruction instruction in method.Body.Instructions)
                    {
                        if (instruction == null || instruction.Operand == null)
                        {
                            continue;
                        }
                        Type operandType = instruction.Operand.GetType();
                        if (!types.Contains(operandType))
                        {
                            types.Add(operandType);
                            toReturn += operandType.FullName + " " + instruction.OpCode.ToString() + "\n";
                        }
                    }
                }
            }
            return toReturn;
        }

        public static void OutputFullTypeDetails(DirectoryInfo directory, TypeDef type)
        {
            byte[] temp;

            FileStream overviewFile = File.Create(directory.FullName + @"\overview.txt");
            string overviewString = "METHODS:\n";
            foreach(MethodDef methodDef in type.Methods)
            {
                overviewString += "\t" + methodDef.FullName + "\n";
            }
            overviewString += "FIELDS:\n";
            foreach (FieldDef fieldDef in type.Fields)
            {
                overviewString += "\t" + fieldDef.FullName + "\n";
            }
            overviewString.Trim();
            temp = new UTF8Encoding(true).GetBytes(overviewString);
            overviewFile.Write(temp, 0, temp.Length);
            overviewFile.Close();

            FileStream fieldsFile = File.Create(directory.FullName + @"\fields.txt");
            string fieldsString = "FIELDS:\n";
            foreach(FieldDef fieldDef in type.Fields)
            {
                fieldsString += "\t";
                fieldsString += GetModifiers(fieldDef.Attributes);
                fieldsString += fieldDef.FieldType.TypeName;
                fieldsString += " ";
                fieldsString += fieldDef.Name;
                fieldsString += " = ";
                fieldsString += GetHex(fieldDef.InitialValue);
                fieldsString += "\n";
            }
            fieldsString.Trim();
            temp = new UTF8Encoding(true).GetBytes(fieldsString);
            fieldsFile.Write(temp, 0, temp.Length);
            fieldsFile.Close();

            DirectoryInfo methodsDirectory = Directory.CreateDirectory(directory.FullName + @"\methods");
            foreach (MethodDef methodDef in type.Methods)
            {
                FileStream methodFile = File.Create(methodsDirectory.FullName + @"\" + MakeFileSystemValidMethod(methodDef.FullName) + ".txt");
                temp = new UTF8Encoding(true).GetBytes(GetMethodString(methodDef));
                methodFile.Write(temp, 0, temp.Length);
                methodFile.Close();
            }
        }

        public static void PrintOverview(ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            Console.WriteLine(GetOverviewString(moddedModule, originalModule));
        }

        public static string GetOverviewString(ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            string toReturn = "Info for module " + originalModule.Name + " - " + moddedModule.Name + ":\n\n";
            toReturn += ("ADDED TYPES:") + "\n";//Leftover from Console.WriteLine()
            var addedTypes = GetAddedTypes(moddedModule, originalModule);
            foreach (var type in addedTypes)
            {
                toReturn += ("\t" + type.FullName) + "\n";
            }
            toReturn += "\n";

            toReturn += ("REMOVED TYPES:") + "\n";
            var removedTypes = GetAddedTypes(originalModule, moddedModule);
            foreach (var type in removedTypes)
            {
                toReturn += ("\t" + type.FullName) + "\n";
            }
            toReturn += "\n";

            toReturn += ("MODIFIED METHODS") + "\n";
            var correspondingTypes = GetCorrespondingTypes(moddedModule, originalModule);
            foreach (var type in correspondingTypes)
            {
                var moddedMethods = GetModdedMethods(type.Item1, type.Item2);
                if (moddedMethods.Count != 0)
                {
                    toReturn += ("\t" + type.Item1.FullName + ":") + "\n";
                }
                foreach (var method in moddedMethods)
                {
                    toReturn += ("\t\t" + method.Item1.FullName) + "\n";
                }
            }
            toReturn += "\n";

            toReturn += ("ADDED METHODS:") + "\n";
            foreach (var type in correspondingTypes)
            {
                var addedMethods = GetAddedMethods(type.Item1, type.Item2);
                if (addedMethods.Count != 0)
                {
                    toReturn += ("\t" + type.Item1.FullName + ":") + "\n";
                }
                foreach (var method in addedMethods)
                {
                    toReturn += ("\t\t" + method.FullName) + "\n";
                }
            }
            toReturn += "\n";

            toReturn += ("REMOVED METHODS:") + "\n";
            foreach (var type in correspondingTypes)
            {
                var addedMethods = GetAddedMethods(type.Item2, type.Item1);
                if (addedMethods.Count != 0)
                {
                    toReturn += ("\t" + type.Item2.FullName + ":") + "\n";
                }
                foreach (var method in addedMethods)
                {
                    toReturn += ("\t\t" + method.FullName) + "\n";
                }
            }
            toReturn += "\n";

            toReturn += ("MODIFIED FIELDS") + "\n";
            foreach (var type in correspondingTypes)
            {
                var moddedFields = GetModdedFields(type.Item1, type.Item2);
                if (moddedFields.Count != 0)
                {
                    toReturn += ("\t" + type.Item1.FullName + ":") + "\n";
                }
                foreach (var field in moddedFields)
                {
                    toReturn += ("\t\t" + field.FullName) + "\n";
                }
            }
            toReturn += "\n";

            toReturn += ("ADDED FIELDS:") + "\n";
            foreach (var type in correspondingTypes)
            {
                var addedFields = GetAddedFields(type.Item1, type.Item2);
                if (addedFields.Count != 0)
                {
                    toReturn += ("\t" + type.Item1.FullName + ":") + "\n";
                }
                foreach (var field in addedFields)
                {
                    toReturn += ("\t\t" + field.FullName) + "\n";
                }
            }
            toReturn += "\n";

            toReturn += ("REMOVED FIELDS:") + "\n";
            foreach (var type in correspondingTypes)
            {
                var addedFields = GetAddedFields(type.Item2, type.Item1);
                if (addedFields.Count != 0)
                {
                    toReturn += ("\t" + type.Item2.FullName + ":") + "\n";
                }
                foreach (var field in addedFields)
                {
                    toReturn += ("\t\t" + field.FullName) + "\n";
                }
            }
            toReturn += "\n";

            return toReturn;
        }

        #endregion
        #region queries

        public static List<TypeDef> GetAddedTypes(ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            var addedTypes = new List<TypeDef>();

            var typeFound = false;

            foreach (var typeDef in moddedModule.GetTypes())
            {
                foreach (var orgTypeDef in originalModule.GetTypes())
                {
                    if (typeDef.FullName == orgTypeDef.FullName)
                    {
                        typeFound = true;
                        break;
                    }
                }

                if (typeFound == false)
                {
                    addedTypes.Add(typeDef);
                }
                else
                {
                    typeFound = false;
                }
            }

            return addedTypes;
        }

        public static List<Tuple<TypeDef, TypeDef>> GetCorrespondingTypes(ModuleDefMD moddedModule, ModuleDefMD originalModule)
        {
            var types = new List<Tuple<TypeDef, TypeDef>>();

            foreach (var typeDef in moddedModule.GetTypes())
            {
                foreach (var orgTypeDef in originalModule.GetTypes())
                {
                    if (typeDef.FullName == orgTypeDef.FullName)
                    {
                        types.Add(new Tuple<TypeDef, TypeDef>(typeDef, orgTypeDef));
                        break;
                    }
                }
            }

            return types;
        }

        public static List<Tuple<MethodDef, MethodDef>> GetModdedMethods(TypeDef moddedType, TypeDef originalType)
        {
            var moddedMethods = new List<Tuple<MethodDef, MethodDef>>();

            foreach (var methodDef in moddedType.Methods)
            {
                foreach (var orgMethodDef in originalType.Methods)
                {
                    if (methodDef.FullName == orgMethodDef.FullName)
                    {
                        if(orgMethodDef.Body == null || methodDef.Body == null)
                        {
                            break;
                        }

                        var orgInstructions = orgMethodDef.Body.Instructions;
                        var moddedInstructions = methodDef.Body.Instructions;

                        if(orgInstructions.Count != moddedInstructions.Count)
                        {
                            moddedMethods.Add(new Tuple<MethodDef, MethodDef>(methodDef, orgMethodDef));
                            break;
                        }

                        bool different = false;

                        for(int i = 0;i < orgInstructions.Count;i++)
                        {
                            if(GetOperandString(orgInstructions[i]) != GetOperandString(moddedInstructions[i]))
                            {
                                different = true;
                                break;
                            }
                            if (GetOpCodeString(orgInstructions[i]) != GetOpCodeString(moddedInstructions[i]))
                            {
                                different = true;
                                break;
                            }
                        }

                        if (different)
                        {
                            moddedMethods.Add(new Tuple<MethodDef, MethodDef>(methodDef, orgMethodDef));
                        }

                        break;
                    }
                }
            }

            return moddedMethods;
        }

        public static List<FieldDef> GetModdedFields(TypeDef moddedType, TypeDef originalType)
        {
            var moddedFields = new List<FieldDef>();

            foreach (var fieldDef in moddedType.Fields)
            {
                foreach (var orgFieldDef in originalType.Fields)
                {
                    if (fieldDef.FullName == orgFieldDef.FullName)
                    {
                        bool different = false;

                        if(fieldDef.InitialValue != orgFieldDef.InitialValue)
                        {
                            different = true;
                        }

                        if (fieldDef.HasConstant != orgFieldDef.HasConstant)
                        {
                            different = true;
                        }

                        if (fieldDef.IsStatic != orgFieldDef.IsStatic)
                        {
                            different = true;
                        }

                        if (fieldDef.Attributes != orgFieldDef.Attributes)
                        {
                            different = true;
                        }

                        if (different)
                        {
                            moddedFields.Add(fieldDef);
                        }

                        break;
                    }
                }
            }

            return moddedFields;
        }

        public static List<MethodDef> GetAddedMethods(TypeDef moddedType, TypeDef originalType)
        {
            var addedMethods = new List<MethodDef>();

            var typeFound = false;

            foreach (var methodDef in moddedType.Methods)
            {
                foreach (var orgmethodDef in originalType.Methods)
                {
                    if (methodDef.FullName == orgmethodDef.FullName)
                    {
                        typeFound = true;
                        break;
                    }
                }

                if (typeFound == false)
                {
                    addedMethods.Add(methodDef);
                }
                else
                {
                    typeFound = false;
                }
            }

            return addedMethods;
        }

        public static List<FieldDef> GetAddedFields(TypeDef moddedType, TypeDef originalType)
        {
            var addedFields = new List<FieldDef>();

            var typeFound = false;

            foreach (var fieldDef in moddedType.Fields)
            {
                foreach (var orgFieldDef in originalType.Fields)
                {
                    if (fieldDef.FullName == orgFieldDef.FullName)
                    {
                        typeFound = true;
                        break;
                    }
                }
                if (typeFound == false)
                {
                    addedFields.Add(fieldDef);
                }
                else
                {
                    typeFound = false;
                }
            }

            return addedFields;
        }

        public static TypeDef GetTypeFromModule(ModuleDefMD module, string name, string @namespace) {
            foreach(TypeDef type in module.Types)
            {
                if (type.Name == name && type.Namespace == @namespace)
                {
                    return type;
                }
            }
            return null;
        }

        #endregion
        #region output

        public static void PrintMethod(MethodDef method)
        {
            Console.Write(GetMethodString(method));
        }

        public static string GetMethodString(MethodDef method, bool includeName = true)
        {
            string toReturn = includeName ? method.FullName + ":\n" : "";
            if (method.Body == null)
            {
                toReturn += "EMPTY";
            }
            else
            {
                foreach (Instruction i in method.Body.Instructions)
                {
                    toReturn += (GetOperandStringForDisplay(i) + " | " + GetOpCodeStringForDisplay(i) + " : " + GetOpCodeCode(i) + " (" + GetOperandTypeEnumString(i) + " " + GetOperandTypeString(i) + ")\n");
                }
                toReturn.Trim();
            }
            return toReturn;
        }

        public static List<Diff> GetMethodDiffs(MethodDef moddedMethod, MethodDef orgMethod)
        {
            var dmp = new diff_match_patch();
            return dmp.diff_lineModeStrict(GetMethodString(orgMethod, false), GetMethodString(moddedMethod, false));
        }

        public static void PrintDifferences(List<Diff> diffs)
        {
            ConsoleColor oldForegroundColor = Console.ForegroundColor;
            ConsoleColor oldBackgroundColor = Console.BackgroundColor;
            int lineNum = 1;
            int prevEqualLineNum = 1;
            foreach (Diff diff in diffs)
            {
                string temp = diff.text.TrimEnd();
                string[] lines = temp.Split('\n');
                int lineCount = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                {
                    int currentLineNum = (diff.operation == Operation.EQUAL ? lineNum : prevEqualLineNum ) + i;
                    string temp2 = ("     " + currentLineNum);
                    string lineString = temp2.Substring(temp2.Length - 4);
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.Write(lineString + ": ");
                    Console.BackgroundColor = ConsoleColor.Black;
                    switch (diff.operation)
                    {
                        case Operation.EQUAL:
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write("   ");
                            break;
                        case Operation.DELETE:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(" - ");
                            break;
                        case Operation.INSERT:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(" + ");
                            break;
                    }
                    Console.WriteLine(lines[i]);
                }
                switch (diff.operation)
                {
                    case Operation.EQUAL:
                        prevEqualLineNum = lineNum + lineCount ;
                        lineNum = prevEqualLineNum;
                        break;
                    case Operation.DELETE:
                        if(prevEqualLineNum + lineCount > lineNum)
                        {
                            lineNum = prevEqualLineNum + lineCount;
                        }
                        break;
                    case Operation.INSERT:
                        if (prevEqualLineNum + lineCount > lineNum)
                        {
                            lineNum = prevEqualLineNum + lineCount;
                        }
                        break;
                }
            }
            Console.ForegroundColor = oldForegroundColor;
            Console.BackgroundColor = oldBackgroundColor;
        }

        public static string GetDifferencesString(List<Diff> diffs)//I know duplicated code is bad but it works so whatever
        {
            int lineNum = 1;
            int prevEqualLineNum = 1;
            string toReturn = "";
            foreach (Diff diff in diffs)
            {
                string temp = diff.text.TrimEnd();
                string[] lines = temp.Split('\n');
                int lineCount = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                {
                    int currentLineNum = (diff.operation == Operation.EQUAL ? lineNum : prevEqualLineNum) + i;
                    string temp2 = ("     " + currentLineNum);
                    string lineString = temp2.Substring(temp2.Length - 4);
                    toReturn += (lineString + ": ");
                    switch (diff.operation)
                    {
                        case Operation.EQUAL:
                            toReturn += ("   ");
                            break;
                        case Operation.DELETE:
                            toReturn += (" - ");
                            break;
                        case Operation.INSERT:
                            toReturn += (" + ");
                            break;
                    }
                    toReturn += (lines[i]) + "\n";
                }
                switch (diff.operation)
                {
                    case Operation.EQUAL:
                        prevEqualLineNum = lineNum + lineCount;
                        lineNum = prevEqualLineNum;
                        break;
                    case Operation.DELETE:
                        if (prevEqualLineNum + lineCount > lineNum)
                        {
                            lineNum = prevEqualLineNum + lineCount;
                        }
                        break;
                    case Operation.INSERT:
                        if (prevEqualLineNum + lineCount > lineNum)
                        {
                            lineNum = prevEqualLineNum + lineCount;
                        }
                        break;
                }
            }
            return toReturn;
        }

        #endregion
        #region helpers
        private static string GetOperandString(Instruction i) => i.Operand != null ? i.Operand.ToString() : "";
        private static string GetOperandStringForDisplay(Instruction i) => i.Operand != null ? i.Operand.ToString() : "NO OPERAND";
        private static string GetOpCodeString(Instruction i) => i.OpCode != null ? i.OpCode.ToString() : "";
        private static string GetOpCodeCode(Instruction i) => i.OpCode != null ? ((ushort) i.OpCode.Code) + "" : "";
        private static string GetOpCodeStringForDisplay(Instruction i) => i.OpCode != null ? i.OpCode.ToString() : "NO OPCODE";
        private static string GetOperandTypeEnumString(Instruction i) => i.OpCode.OperandType.ToString();
        private static string GetOperandTypeString(Instruction i) => i.Operand != null ? i.Operand.GetType().Name : "NO OPERAND";
        private static string MakeFileSystemValidType(string s) => MakeFileSystemValid(s).Replace(")(", @")\(").Replace('.', '\\').Replace(' ', '-');
        private static string MakeFileSystemValid(string s) => s.Replace('<', '(').Replace('>', ')').Replace("`", "").Replace('/', '\\');
        private static string MakeFileSystemValidMethod(string s) => MakeFileSystemValid(s).Replace(@"\", "").Replace("::.", ".").Replace("::", ".");
        private static string GetHex(byte[] raw) => "0x" + (raw == null ? "0000000000000000" : BitConverter.ToString(raw).Replace("-", ""));
        public static List<Type> GetOperandTypes(ModuleDef module) => GetOperandTypes(new List<Type>(), module);
        public static Save LoadFromFile(string filePath) => JsonConvert.DeserializeObject<Save>(File.ReadAllText(filePath), new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto});
        #endregion
        #region modifiers
        //I *think* most of these are right, if not, tell me. It only affects display output so it *should* be fine, even if they are wrong.
        private static string GetModifiers(FieldAttributes attributes) => LiteralAttribute(attributes) + AssemblyAttribute(attributes) + PrivateAttribute(attributes) + ProtectedAttribute(attributes) + PublicAttribute(attributes) + SerializedAttribute(attributes) + StaticAttribute(attributes) + InitOnlyAttribute(attributes);
        //God forgive me for that monstrosity. It worked better in my head when there were less attributes.
        private static string PublicAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.Public) != 0 ? "public " : "";
        private static string AssemblyAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.Assembly) != 0 ? "assembly " : "";
        private static string ProtectedAttribute(FieldAttributes attributes) => ((attributes & FieldAttributes.Family) != 0 && (attributes & FieldAttributes.Public) == 0 && (attributes & FieldAttributes.Private) == 0) ? "protected " : "";
        //I think this is protected? dnlib source code comments say "Accessible only by type and sub-types" which seems like protected, yet it appears with public or private.
        //Looking at how the values are laid out, public will always be family, which I guess makes sense.
        private static string PrivateAttribute(FieldAttributes attributes) => ((attributes & FieldAttributes.Private) != 0 || (attributes & FieldAttributes.PrivateScope) != 0) ? "private " : "";
        private static string StaticAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.Static) != 0 ? "static " : "";
        private static string InitOnlyAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.InitOnly) != 0 ? "readonly " : "";
        private static string SerializedAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.NotSerialized) == 0 ? "serialized " : "";
        private static string LiteralAttribute(FieldAttributes attributes) => (attributes & FieldAttributes.InitOnly) != 0 ? "literal " : "";
        #endregion
        #region converters
        public static T[] ToArray<T>(List<T> list) => list.ToArray();
        public static T[] ToArray<T>(IList<T> list) => ToArray(ToList(list));
        public static List<T> ToList<T>(IList<T> list) => new List<T>(list);
        public static byte[] GetHash(string inputString)//A cryptographic hash is excessively, as this just needs to be a sanity check, but it was the easiest solution.
        {
            using (HashAlgorithm algorithm = SHA256.Create()) return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
        public static string ByteArrayToString(byte[] array) => BitConverter.ToString(array).Replace("-", "").ToLower();
        #endregion
    }
}
