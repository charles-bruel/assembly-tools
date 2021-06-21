﻿using DiffMatchPatch;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
                        removedFieldsToSave.Add(fieldDef.FullName);
                    }
                    toAdd.RemovedFields = removedFieldsToSave.ToArray();

                    List<string> removedMethodsToSave = new List<string>(addedMethods.Count);
                    foreach (MethodDef methodDef in removedMethods)
                    {
                        removedMethodsToSave.Add(methodDef.FullName);
                    }
                    toAdd.RemovedMethods = removedMethodsToSave.ToArray();

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
                typeSave.RemovedMethods = new string[0];
                typeSave.RemovedFields = new string[0];

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
            MethodSave toReturn = GetMethodSaveAttributeSection(methodDef);

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
            ApplySaveStage1(module, save);//Add new types

            ApplySaveStage2(module, save);//Add new methods and fields

            ApplySaveStage3(module, save);//Modify methods
        }

        private static void ApplySaveStage1(ModuleDefMD module, Save save)
        {
            Queue<TypeSave> toAdd = new Queue<TypeSave>(save.AddedTypes);
            while (toAdd.Count != 0)//Add types
            {
                TypeSave typeSave = toAdd.Dequeue();

                ITypeDefOrRef baseClass = GetType(typeSave.BaseType, module);

                TypeDefUser newType = (baseClass == null) ? new TypeDefUser(typeSave.Namespace, typeSave.Name) : new TypeDefUser(typeSave.Namespace, typeSave.Name, baseClass);
                newType.Attributes = typeSave.Attributes;
                module.Types.Add(newType);

            }
        }

        private static void ApplySaveStage2(ModuleDefMD module, Save save)
        {
            foreach (TypeSave modifiedType in save.ModifiedTypes)
            {
                TypeDef existingType = Utils.GetTypeFromModule(module, modifiedType.Name, modifiedType.Namespace);

                ITypeDefOrRef baseClass = GetType(modifiedType.BaseType, module);

                if (baseClass != null) existingType.BaseType = baseClass;

                existingType.Attributes = modifiedType.Attributes;

                ApplySaveToTypeStage2(existingType, modifiedType, module);
            }

            foreach (string removedType in save.RemovedTypes)//O(n^3) go brrrrrr
            {
                foreach (TypeDef type in module.Types)
                {
                    if (removedType == type.FullName)
                    {
                        module.Types.Remove(type);
                        break;
                    }
                }
            }
        }

        private static void ApplySaveStage3(ModuleDefMD module, Save save)
        {
            foreach (TypeSave modifiedType in save.ModifiedTypes)
            {
                TypeDef existingType = Utils.GetTypeFromModule(module, modifiedType.Name, modifiedType.Namespace);

                ApplySaveToTypeStage3(existingType, modifiedType, module);
            }
        }
        
        private static void ApplySaveToTypeStage2(TypeDef type, TypeSave save, ModuleDefMD module)
        {
            foreach(string removedMethod in save.RemovedMethods)
            {
                foreach (MethodDef methodDef in type.Methods)
                {
                    if(removedMethod == methodDef.FullName)
                    {
                        type.Methods.Remove(methodDef);
                        break;
                    }
                }
            }

            foreach (string removedField in save.RemovedFields)
            {
                foreach (FieldDef fieldDef in type.Fields)
                {
                    if (removedField == fieldDef.FullName)
                    {
                        type.Fields.Remove(fieldDef);
                        break;
                    }
                }
            }

            for(int i = 0;i < save.AddedMethods.Length;i++)
            {
                MethodDef temp = GetAddedMethod(save.AddedMethods[i], module);
                type.Methods.Add(temp);
                save.AddedMethods[i].FullName = temp.FullName;//Update name for next section.
                //The fact that this has to be modified indicates something is going wrong somewhere.
            }
        }

        private static void ApplySaveToTypeStage3(TypeDef type, TypeSave save, ModuleDefMD module)
        {
            foreach (MethodSave addedMethod in save.AddedMethods)
            {
                MethodDef addedMethodDef = null;
                foreach(MethodDef methodDef in type.Methods)
                {
                    if (methodDef.FullName == addedMethod.FullName)
                    {
                        addedMethodDef = methodDef;
                    }
                }

                if(addedMethodDef == null)
                {
                    Console.WriteLine("Failed to find: " + addedMethod.FullName);
                } 
                else
                {
                    ApplyInstructionsSaveToMehtod(addedMethodDef, addedMethod, module);
                }
            }
        }

        private static void ApplyInstructionsSaveToMehtod(MethodDef method, MethodSave save, ModuleDefMD module)
        {
            if(method.Body == null)
            {
                method.Body = new CilBody();
            }

            int oldMethodLineCounter = 0;
            List<Instruction> newInstructions = new List<Instruction>();
            List<Instruction> oldInstructions = Utils.ToList(method.Body.Instructions);

            List<Tuple<int,InstructionSave>> indicesToRevisit = new List<Tuple<int, InstructionSave>>();

            foreach (MethodDataBlockSave dataBlock in save.Data)
            {
                switch (dataBlock.Type)
                {
                    case MethodBlockType.KEEP:
                        for(int i = 0;i < dataBlock.Lines;i ++)
                        {
                            newInstructions.Add(oldInstructions[oldMethodLineCounter]);
                            oldMethodLineCounter++;
                        }
                        break;
                    case MethodBlockType.DELETE:
                        oldMethodLineCounter += dataBlock.Lines;
                        break;
                    case MethodBlockType.INSERT:
                        foreach(InstructionSave instructionSave in dataBlock.Data)
                        {
                            OpCode opCode = OpCodes.Nop;
                            if (instructionSave.OpCode >> 8 == 0)
                            {
                                opCode = OpCodes.OneByteOpCodes[(int)((byte)instructionSave.OpCode)];//Its a bit weird, but it was taken from the OpCode constructor decompiled with dnSpy, so I think its right.
                            } 
                            else
                            {
                                opCode = OpCodes.TwoByteOpCodes[(int)((byte)instructionSave.OpCode)];
                            }
                            if(instructionSave.Operand is OperandNoOP)
                            {
                                Instruction instruction = new Instruction(opCode);
                                newInstructions.Add(instruction);
                            } 
                            else
                            {
                                object operand = null;
                                if (instructionSave.Operand is OperandInstruction)//Jumping is fun
                                {
                                    OperandInstruction operandInstruction = (OperandInstruction) instructionSave.Operand;//Cannot use as keyword here because its a struct, which is a shame because it is much cleaner.
                                    if(operandInstruction.Type == OperandInstructionType.EXTERNAL)//Its referencing an instruction in the original method, so it can be assigned now.
                                    {
                                        operand = oldInstructions[operandInstruction.Value];
                                    } 
                                    else//Its referencing a instruction in the new method
                                    {
                                        int currentIndex = newInstructions.Count;
                                        if(operandInstruction.Value < currentIndex)//Its an earlier instruction its referencing, so we can populate it now.
                                        {
                                            operand = newInstructions[operandInstruction.Value];
                                        }
                                        else
                                        {
                                            indicesToRevisit.Add(new Tuple<int,InstructionSave>(currentIndex,instructionSave));//Its an later instruction its referencing, so we have to populate it later.
                                        }
                                    }
                                }
                                else if (instructionSave.Operand is OperandInstructionArray)
                                {
                                    throw new NotImplementedException();
                                }
                                else
                                {
                                    operand = instructionSave.Operand.GetValue(module);
                                }
                                Instruction instruction = new Instruction(opCode, operand);
                                newInstructions.Add(instruction);
                            }
                        }
                        break;
                }
            }

            for(int i = 0;i < indicesToRevisit.Count;i++)
            {
                object operand = null;
                if (indicesToRevisit[i].Item2.Operand is OperandInstruction)
                {
                    OperandInstruction operandInstruction = (OperandInstruction)indicesToRevisit[i].Item2.Operand;//Cannot use as keyword here because its a struct, which is a shame because it is much cleaner
                    if (operandInstruction.Type == OperandInstructionType.EXTERNAL)//Should have already done this, but still will support it.
                    {
                        operand = oldInstructions[operandInstruction.Value];
                    }
                    else
                    {
                        operand = newInstructions[operandInstruction.Value];
                    }
                }

                newInstructions[indicesToRevisit[i].Item1].Operand = operand;
            }

            method.Body.MaxStack = save.StackSize;

            method.Body.KeepOldMaxStack = true;

            method.Body.Instructions.Clear();//Inefficient but it works
            foreach (Instruction instruction in newInstructions)
            {
                method.Body.Instructions.Add(instruction);
            }
        }

        public static MethodDefUser GetAddedMethod(MethodSave method, ModuleDefMD module)//Just the signature, does not populate the body.
        {
            MethodSig sig = null;
            if ((method.Attributes & MethodAttributes.Static) == 0)//Not static
            {
                TypeSig returnType = new ClassSig(GetType(method.ReturnType, module));//Need to do proper choosing at some point
                TypeSig[] parameters = new TypeSig[method.Parameters.Length];
                for(int i = 0;i < method.Parameters.Length;i ++)
                {
                    parameters[i] = GetSig(method.Parameters[i].Type, module);
                }
                sig = MethodSig.CreateInstance(returnType, parameters);
            }
            else//Static
            {
                TypeSig returnType = new ClassSig(GetType(method.ReturnType, module));//Need to do proper choosing at some point
                TypeSig[] parameters = new TypeSig[method.Parameters.Length];
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    parameters[i] = GetSig(method.Parameters[i].Type, module);
                }
                sig = MethodSig.CreateStatic(returnType, parameters);
            }
            MethodDefUser methodDef = new MethodDefUser(method.Name, sig, method.Attributes);

            methodDef.Attributes = method.Attributes;

            for (int i = 0;i < methodDef.Parameters.Count-1;i ++)
            {
                methodDef.ParamDefs.Add(new ParamDefUser(method.Parameters[i].Name, method.Parameters[i].Sequence, method.Parameters[i].Attributes));
            }

            return methodDef;
        }

        public static TypeSig GetSig(TypeRefSave type, ModuleDefMD module)
        {
            Regex rx = new Regex(@"\S*\[[0.,]+\]");
            string typeName = type.Name;
            int typeNameLength = typeName.Length;
            if(typeNameLength > 2 && typeName[typeNameLength - 2] == '[' && typeName[typeNameLength - 1] == ']')//It ends in [], so its an array
            {
                TypeRefSave arrayType = type;//Copy
                arrayType.Name = typeName.Substring(0, typeNameLength - 2);
                return new ArraySig(GetSig(arrayType, module));
            } 
            else if (rx.Matches(typeName).Count != 0)//Multirank array, i.e. Int32[,]
            {
                int rank = typeName.Split(",").Length;
                TypeRefSave arrayType = type;//Copy
                arrayType.Name = typeName.Substring(0, typeName.IndexOf("["));
                return new ArraySig(GetSig(arrayType, module), rank);
            }
            else if (type.GenericParameters.Length != 0)
            {
                TypeRefSave genericBaseType = type;//Copy
                genericBaseType.GenericParameters = new TypeRefSave[0];
                TypeSig[] genericParams = new TypeSig[type.GenericParameters.Length];
                for(int i = 0;i < genericParams.Length;i++)
                {
                    genericParams[i] = GetSig(type.GenericParameters[i], module);
                }
                return new GenericInstSig((ClassOrValueTypeSig)GetSig(genericBaseType, module), genericParams);//I *think* this cast is ok
            }
            return new ClassSig(GetType(type, module));
        }

        public static ITypeDefOrRef GetType(TypeRefSave? target, ModuleDefMD module)
        {
            ITypeDefOrRef type = null;
            if (target.HasValue)
            {
                if(target.Value.DefiningAssembly != "netstandard" && target.Value.DefiningAssembly != "mscorlib" && target.Value.DefiningAssembly != module.Assembly.Name)
                {
                    type = new TypeRefUser(module, target.Value.Namespace, target.Value.Name, new AssemblyRefUser(target.Value.DefiningAssembly));
                }
                else
                {
                    TypeDef temp = Utils.GetTypeFromModule(module, target.Value.Name, target.Value.Namespace);
                    if (temp == null)
                    {
                        type = module.CorLibTypes.GetTypeRef(target.Value.Namespace, target.Value.Name);
                    }
                    else
                    {
                        type = temp;
                    }
                }
            }

            return type;
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

        //This is just the attributes (name, signature, etc)
        public static MethodSave GetMethodSaveAttributeSection(MethodDef method)
        {
            MethodSave toReturn = new MethodSave();

            toReturn.Name = method.Name;
            toReturn.FullName = method.FullName;
            toReturn.Attributes = method.Attributes;

            toReturn.ReturnType = TypeRefSave.Get(method.MethodSig.RetType);

            toReturn.StackSize = method.Body.MaxStack;

            int actualParameterCount = 0;

            foreach(Parameter parameter in method.Parameters)
            {
                if (parameter.IsNormalMethodParameter)
                {
                    actualParameterCount++;
                }
            }

            toReturn.Parameters = new ParameterSave[actualParameterCount];


            int currentIndex = 0;
            foreach (Parameter parameter in method.Parameters)
            {
                if (parameter.IsNormalMethodParameter)
                {
                    toReturn.Parameters[currentIndex] = new ParameterSave();
                    toReturn.Parameters[currentIndex].Type = TypeRefSave.Get(parameter.Type);
                    toReturn.Parameters[currentIndex].Name = parameter.Name;
                    toReturn.Parameters[currentIndex].Attributes = parameter.ParamDef.Attributes;
                    toReturn.Parameters[currentIndex].Sequence = parameter.ParamDef.Sequence;
                    currentIndex++;
                }
            }

            return toReturn;
        }

        public static MethodSave GetModifiedMethodSave(MethodDef original, MethodDef modded)
        {
            MethodSave toReturn = GetMethodSaveAttributeSection(modded);

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

    public struct TypeSave
    {
        public string Name;
        public string Namespace;
        public TypeRefSave? BaseType;
        public TypeAttributes Attributes;
        public MethodSave[] ModifiedMethods;
        public MethodSave[] AddedMethods;
        public string[] RemovedMethods;
        public FieldSave[] ModifiedFields;
        public FieldSave[] AddedFields;
        public string[] RemovedFields;
    }

    public struct MethodSave
    {
        public string Name;
        public string FullName;
        public MethodAttributes Attributes;
        public TypeRefSave ReturnType;
        public ParameterSave[] Parameters;
        public MethodDataBlockSave[] Data;
        public string OriginalMethodHash;
        public ushort StackSize;
    }

    public struct ParameterSave
    {
        public TypeRefSave Type;
        public string Name;
        public ushort Sequence;//Not exactly sure what this does.
        public ParamAttributes Attributes;
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
}
