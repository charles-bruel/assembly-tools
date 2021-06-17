using DiffMatchPatch;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace AssemblyTools
{
    public class SaveUtils
    {
        public static Save GetSave(ModuleDefMD original, ModuleDefMD modded, string name)
        {
            Save toReturn = new Save();

            toReturn.Name = name;
            toReturn.ASMN1 = original.Name;
            toReturn.ASMN2 = modded.Name;

            List<TypeDef> removedTypes = Utils.GetAddedTypes(original, modded);
            List<string> removedTypesNames = new List<string>(removedTypes.Count);
            foreach (TypeDef def in removedTypes)
            {
                removedTypesNames.Add(def.FullName);
            }

            toReturn.ModifiedTypes = GetModifiedTypesSave(original, modded).ToArray();
            toReturn.AddedTypes = GetAddedTypesSave(original, modded).ToArray();
            toReturn.RemovedTypes = removedTypesNames.ToArray();

            return toReturn;
        }

        public static List<TypeSave> GetModifiedTypesSave(ModuleDefMD original, ModuleDefMD modded)
        {
            List<TypeSave> toReturn = new List<TypeSave>();

            List<Tuple<TypeDef, TypeDef>> correspondingTypes = Utils.GetCorrespondingTypes(modded, original);
            foreach(Tuple<TypeDef, TypeDef> type in correspondingTypes)
            {
                //Basic things
                List<FieldDef> addedFields = Utils.GetAddedFields(type.Item1, type.Item2);
                List<FieldDef> modifiedFields = Utils.GetModdedFields(type.Item1, type.Item2);
                List<FieldDef> removedFields = Utils.GetAddedFields(type.Item2, type.Item1);
                List<MethodDef> addedMethods = Utils.GetAddedMethods(type.Item1, type.Item2);
                List<Tuple<MethodDef, MethodDef>> modifiedMethods = Utils.GetModdedMethods(type.Item1, type.Item2);
                List<MethodDef> removedMethods = Utils.GetAddedMethods(type.Item2, type.Item1);

                bool modifiedMembers = addedFields.Count != 0 || modifiedFields.Count != 0 || removedFields.Count != 0 || addedMethods.Count != 0 || modifiedMethods.Count != 0 || removedMethods.Count != 0;
                bool modifiedSignature = (type.Item1.BaseType != null && type.Item1.BaseType.FullName != type.Item2.BaseType.FullName) || type.Item1.Attributes != type.Item2.Attributes;//May have been things I missed in here

                if (modifiedMembers || modifiedSignature)//long boi 2
                {
                    TypeSave toAdd = new TypeSave();
                    toAdd.Attributes = type.Item1.Attributes;
                    toAdd.Name = type.Item1.Name;
                    toAdd.Namespace = type.Item1.Namespace;

                    if (type.Item1.BaseType.Name != "Object")
                    {
                        toAdd.BaseType = TypeRefSave.Get(type.Item1.BaseType);
                    }
                    else
                    {
                        toAdd.BaseType = null;
                    }

                    List<FieldSave> addedFieldsToSave = new List<FieldSave>(addedFields.Count);
                    foreach (FieldDef fieldDef in addedFields)
                    {
                        addedFieldsToSave.Add(GetFieldSave(fieldDef));
                    }
                    toAdd.AddedFields = addedFieldsToSave.ToArray();

                    List<MethodSave> addedMethodsToSave = new List<MethodSave>(addedMethods.Count);
                    foreach (MethodDef methodDef in addedMethods)
                    {
                        addedMethodsToSave.Add(GetMethodSave(methodDef));
                    }
                    toAdd.AddedMethods = addedMethodsToSave.ToArray();

                    List<string> removedFieldsToSave = new List<string>(removedFields.Count);
                    foreach (FieldDef fieldDef in removedFields)
                    {
                        removedFieldsToSave.Add(fieldDef.Name);
                    }
                    toAdd.removedFields = removedFieldsToSave.ToArray();

                    List<string> removedMethodsToSave = new List<string>(addedMethods.Count);
                    foreach (MethodDef methodDef in removedMethods)
                    {
                        removedMethodsToSave.Add(methodDef.Name);
                    }
                    toAdd.removedMethods = removedMethodsToSave.ToArray();

                    List<FieldSave> modifiedFieldsToSave = new List<FieldSave>(modifiedFields.Count);//I am *pretty* sure this works fine
                    foreach (FieldDef fieldDef in modifiedFields)
                    {
                        modifiedFieldsToSave.Add(GetFieldSave(fieldDef));
                    }
                    toAdd.ModifiedFields = modifiedFieldsToSave.ToArray();

                    List<MethodSave> modifiedMethodsToSave = new List<MethodSave>(modifiedMethods.Count);
                    foreach(var methodDef in modifiedMethods)
                    {
                        modifiedMethodsToSave.Add(GetModifiedMethodSave(methodDef.Item1, methodDef.Item2));
                    }
                    toAdd.ModifiedMethods = modifiedMethodsToSave.ToArray();

                    toReturn.Add(toAdd);
                }

            }
            return toReturn;
        }

        public static List<TypeSave> GetAddedTypesSave(ModuleDefMD original, ModuleDefMD modded)
        {
            List<TypeDef> addedTypes = Utils.GetAddedTypes(modded, original);
            List<TypeSave> toReturn = new List<TypeSave>(addedTypes.Count);
            foreach (TypeDef type in addedTypes)
            {
                TypeSave typeSave = new TypeSave();
                typeSave.Name = type.Name;
                typeSave.Namespace = type.Namespace;
                typeSave.Attributes = type.Attributes;

                typeSave.BaseType = TypeRefSave.Get(type.BaseType);

                typeSave.ModifiedMethods = new MethodSave[0];
                typeSave.ModifiedFields = new FieldSave[0];
                typeSave.removedMethods = new string[0];
                typeSave.removedFields = new string[0];

                List<MethodSave> methods = new List<MethodSave>(type.Methods.Count);
                foreach (MethodDef methodDef in type.Methods)
                {
                    methods.Add(GetMethodSave(methodDef));
                }
                typeSave.AddedMethods = methods.ToArray();

                List<FieldSave> fields = new List<FieldSave>(type.Fields.Count);
                foreach (FieldDef fieldDef in type.Fields)
                {
                    fields.Add(GetFieldSave(fieldDef));
                }
                typeSave.AddedFields = fields.ToArray();

                toReturn.Add(typeSave);
            }
            return toReturn;
        }

        public static MethodSave GetMethodSave(MethodDef methodDef)
        {
            MethodSave toReturn = new MethodSave();

            toReturn.Name = methodDef.Name;
            toReturn.Attributes = methodDef.Attributes;

            toReturn.ReturnType = TypeRefSave.Get(methodDef.MethodSig.RetType);
            toReturn.Parameters = new TypeRefSave[methodDef.Parameters.Count];
            toReturn.ParameterNames = new string[methodDef.Parameters.Count];
            for (int i = 0; i < methodDef.Parameters.Count; i++)
            {
                toReturn.Parameters[i] = TypeRefSave.Get(methodDef.Parameters[i].Type);
                toReturn.ParameterNames[i] = methodDef.Parameters[i].Name;
            }

            //Since its an added method, its one big block of data
            if (methodDef.Body == null || methodDef.Body.Instructions == null)
            {
                toReturn.Data = new MethodDataBlockSave[0];
            }
            else
            {
                toReturn.OriginalMethodHash = Utils.ByteArrayToString(Utils.GetHash(Utils.GetMethodString(methodDef, true)));

                InstructionSave[] instructions = GetInstructionsSave(Utils.ToArray(methodDef.Body.Instructions));

                MethodDataBlockSave data = new MethodDataBlockSave();
                data.Type = MethodBlockType.INSERT;
                data.Data = instructions;
                data.Lines = instructions.Length;
                toReturn.Data = new MethodDataBlockSave[] { data };
            }

            return toReturn;
        }

        public static void ApplySave(ModuleDefMD module, string saveFilePath)
        {
            ApplySave(module, Utils.LoadFromFile(saveFilePath));
        }

        public static void ApplySave(ModuleDefMD module, Save save)
        {
            Queue<TypeSave> toAdd = new Queue<TypeSave>(save.AddedTypes);
            while (toAdd.Count != 0)//Add types
            {
                TypeSave typeSave = toAdd.Dequeue();
                ITypeDefOrRef baseClass = null;
                bool success = true;
                if (typeSave.BaseType.HasValue)
                {
                    if ((baseClass = module.CorLibTypes.GetTypeRef(typeSave.BaseType.Value.Namespace, typeSave.BaseType.Value.Name)) == null)//Its NOT a core type like string
                    {
                        if((baseClass = Utils.GetTypeFromModule(module, typeSave.BaseType.Value.Name, typeSave.BaseType.Value.Namespace)) == null)
                        {
                            Console.WriteLine("COULD NOT FIND VALID BASE TYPE:");
                            Console.WriteLine(typeSave.BaseType.Value.Namespace + "." + typeSave.BaseType.Value.Name);
                            //This has an issue. If the base type is loaded afterwords, it wont find it even though it should. There is a bit of work to make it work (the queue) but it hasn't been fully implemented.
                        }
                    }
                }

                if (success)
                {
                    TypeDefUser newType = (baseClass == null) ? new TypeDefUser(typeSave.Namespace, typeSave.Name) : new TypeDefUser(typeSave.Namespace, typeSave.Name, baseClass);
                    // Console.WriteLine(newType.BaseType);
                    newType.Attributes = typeSave.Attributes;
                    module.Types.Add(newType);
                }
            }
        }

        public static InstructionSave[] GetInstructionsSave(Instruction[] rawInstructions, Instruction[] secondaryInstructions = null)
        {
            return GetInstructionsSave(rawInstructions, 0, rawInstructions.Length - 1, secondaryInstructions);
        }

        public static InstructionSave[] GetInstructionsSave(Instruction[] rawInstructions, int start, int end, Instruction[] secondaryInstructions = null)//Indices are inclusive
        {
            InstructionSave[] instructions = new InstructionSave[rawInstructions.Length];

            if(end >= rawInstructions.Length)
            {
                throw new ArgumentException("End value cannot exceed size of instruction array. (" + end + " > " + rawInstructions.Length + ")");
            }
            if (start < 0)
            {
                throw new ArgumentException("Start value cannot be negative. (" + start + " < 0)");
            }
            if(start > end)
            {
                throw new ArgumentException("Start value cannot be larger then end value. (" + start + " > " + end + ")");
            }

            for (int i = start; i < end + 1; i++)
            {
                InstructionSave instructionSave = new InstructionSave();
                instructionSave.Operand = OperandUtils.GetOperandSave(rawInstructions[i], rawInstructions, secondaryInstructions);
                instructionSave.OpCode = (int)rawInstructions[i].OpCode.Code;
                instructions[i] = instructionSave;
            }

            return instructions;
        }

        public static MethodSave GetModifiedMethodSave(MethodDef original, MethodDef modded)
        {
            MethodSave toReturn = new MethodSave();

            toReturn.Name = modded.Name;
            toReturn.Attributes = modded.Attributes;

            toReturn.ReturnType = TypeRefSave.Get(modded.MethodSig.RetType);
            toReturn.Parameters = new TypeRefSave[modded.Parameters.Count];
            toReturn.ParameterNames = new string[modded.Parameters.Count];

            for (int i = 0; i < modded.Parameters.Count; i++)
            {
                toReturn.Parameters[i] = TypeRefSave.Get(modded.Parameters[i].Type);
                toReturn.ParameterNames[i] = modded.Parameters[i].Name;
            }

            //Since its an added method, its one big block of data
            if (original.Body == null || original.Body.Instructions == null)
            {
                if (modded.Body == null || modded.Body.Instructions == null)//BOTH ARE EMPTY
                {
                    toReturn.Data = new MethodDataBlockSave[0];
                }
                else//No body then body
                {//Use standard behaviour
                    Instruction[] rawInstructions = new List<Instruction>(modded.Body.Instructions).ToArray();
                    InstructionSave[] instructions = new InstructionSave[rawInstructions.Length];

                    for (int i = 0; i < rawInstructions.Length; i++)
                    {
                        InstructionSave instructionSave = new InstructionSave();
                        instructionSave.Operand = OperandUtils.GetOperandSave(rawInstructions[i], rawInstructions);
                        instructionSave.OpCode = (int)rawInstructions[i].OpCode.Code;
                        instructions[i] = instructionSave;
                    }

                    MethodDataBlockSave data = new MethodDataBlockSave();
                    data.Type = MethodBlockType.INSERT;
                    data.Data = instructions;
                    data.Lines = instructions.Length;
                    toReturn.Data = new MethodDataBlockSave[] { data };
                }
            }
            else if (original.Body != null && original.Body.Instructions != null)
            {
                toReturn.OriginalMethodHash = Utils.ByteArrayToString(Utils.GetHash(Utils.GetMethodString(original, true)));

                if (modded.Body == null || modded.Body.Instructions == null)//Had a body then deleted.
                {
                    throw new ArgumentException("Method body went from existing to gone.");//Fail
                }
                else//Normal
                {
                    List<Diff> diffs = Utils.GetMethodDiffs(modded, original);
                    //So we now have a text representation of the instructions, because DiffMatchPatch uses strings.
                    //We don't actually care about the text, just the size of each block.
                    //So this is very inefficient and possibly prone to mistakes, as the text representation is much less accurate.
                    //TODO: FIXME (maybe)

                    int origInstructionCount = -1;
                    int moddedInstructionCount = -1;

                    List<MethodDataBlockSave> data = new List<MethodDataBlockSave>(diffs.Count);

                    foreach (Diff diff in diffs)
                    {
                        int lines = diff.text.Trim().Split('\n').Length;

                        int origEndInstructionCount = origInstructionCount;
                        int moddedEndInstructionCount = moddedInstructionCount;

                        switch (diff.operation)
                        {
                            case Operation.EQUAL:
                                origEndInstructionCount += lines;
                                moddedEndInstructionCount += lines;

                                MethodDataBlockSave tempDataBlockE = new MethodDataBlockSave();

                                tempDataBlockE.Type = MethodBlockType.KEEP;
                                tempDataBlockE.Lines = lines;
                                tempDataBlockE.Data = null;
                                //No need to set data

                                data.Add(tempDataBlockE);
                                break;
                            case Operation.DELETE:
                                origEndInstructionCount += lines;

                                MethodDataBlockSave tempDataBlockD = new MethodDataBlockSave();

                                tempDataBlockD.Type = MethodBlockType.DELETE;
                                tempDataBlockD.Lines = lines;
                                tempDataBlockD.Data = null;
                                //No need to set data

                                data.Add(tempDataBlockD);
                                break;
                            case Operation.INSERT://The complex one
                                moddedEndInstructionCount += lines;

                                MethodDataBlockSave tempDataBlockI = new MethodDataBlockSave();

                                tempDataBlockI.Type = MethodBlockType.DELETE;
                                tempDataBlockI.Lines = lines;
                                tempDataBlockI.Data = GetInstructionsSave(Utils.ToArray(modded.Body.Instructions), moddedInstructionCount + 1, moddedEndInstructionCount, Utils.ToArray(original.Body.Instructions));

                                data.Add(tempDataBlockI);
                                break;
                        }

                        origInstructionCount = origEndInstructionCount;
                        moddedInstructionCount = moddedEndInstructionCount;
                    }

                }
            }
            return toReturn;
        }

        public static FieldSave GetFieldSave(FieldDef fieldDef)
        {
            FieldSave toReturn = new FieldSave();

            toReturn.Attributes = fieldDef.Attributes;
            toReturn.Data = fieldDef.InitialValue;
            toReturn.Name = fieldDef.Name;
            toReturn.ElementType = fieldDef.ElementType;
            toReturn.Type = TypeRefSave.Get(fieldDef.FieldSig.Type);

            return toReturn;
        }
    }

    public struct Save
    {
        public string Name;
        public string ASMN1;
        public string ASMN2;

        public TypeSave[] AddedTypes;
        public TypeSave[] ModifiedTypes;
        public string[] RemovedTypes;
    }

    public struct TypeSave//Needs to be nullable and contain itself
    {
        public string Name;
        public string Namespace;
        public TypeRefSave? BaseType;
        public TypeAttributes Attributes;
        public MethodSave[] ModifiedMethods;
        public MethodSave[] AddedMethods;
        public string[] removedMethods;
        public FieldSave[] ModifiedFields;
        public FieldSave[] AddedFields;
        public string[] removedFields;
    }

    public struct MethodSave
    {
        public string Name;
        public MethodAttributes Attributes;
        public TypeRefSave ReturnType;
        public TypeRefSave[] Parameters;
        public string[] ParameterNames;
        public MethodDataBlockSave[] Data;
        public string OriginalMethodHash;
    }

    public struct MethodDataBlockSave
    {
        public int Lines;
        public MethodBlockType Type;
        public InstructionSave[] Data;//This will be empty if Type is KEEP or DELETE
    }

    public struct InstructionSave
    {
        public int OpCode;
        public OperandSave Operand;
    }

    public struct FieldSave
    {
        public string Name;
        public FieldAttributes Attributes;
        public byte[] Data;
        public ElementType ElementType;
        public TypeRefSave Type;
    }

    public enum MethodBlockType
    {
        INSERT, KEEP, DELETE
    }

    public enum TypeType//Uh does anybody have a better name for the type of a type?
    {
        CLASS, ENUM, INTERFACE
    }
}
