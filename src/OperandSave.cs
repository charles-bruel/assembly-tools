using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Linq;
using System.Collections.Generic;
using System;

namespace AssemblyTools
{
    public class OperandUtils
    {
        public static OperandSave GetOperandSave(Instruction instruction, Instruction[] mainContext, Instruction[] secondaryContext = null)
        {
            object operand = instruction.Operand;
            return GetOperand(operand, mainContext, secondaryContext);
        }

        public static OperandSave GetOperand(object operand, Instruction[] mainContext = null, Instruction[] secondaryContext = null)
        {
            if (operand == null) return new OperandNoOP();
            switch (operand.GetType().FullName)//Do something not awful here
            {
                case "dnlib.DotNet.MemberRefMD":
                case "dnlib.DotNet.MemberRefUser":
                    var temp = (operand as MemberRef);
                    if (temp.IsMethodRef)
                    {
                        return MethodRefSave.Get(operand);
                    }
                    if (temp.IsFieldRef)
                    {
                        return FieldRefSave.Get(operand);
                    }
                    throw new NotImplementedException();
                case "dnlib.DotNet.MethodDefMD":
                case "dnlib.DotNet.MethodRefUser":
                case "dnlib.DotNet.MethodSpecMD":
                    return MethodRefSave.Get(operand);
                case "System.String":
                    return new OperandString(operand as string);
                case "System.SByte":
                    return new OperandSByte((sbyte)operand);
                case "System.Single":
                    return new OperandFloat32((float)operand);
                case "System.Double":
                    return new OperandFloat64((double)operand);
                case "System.Int32":
                    return new OperandInt32((int)operand);
                case "System.Int64":
                    return new OperandInt64((long)operand);
                case "dnlib.DotNet.TypeDefMD":
                case "dnlib.DotNet.TypeRefMD":
                case "dnlib.DotNet.TypeRefUser":
                case "dnlib.DotNet.TypeSpecMD":
                    return TypeRefSave.Get(operand);
                case "dnlib.DotNet.FieldDefMD":
                    return FieldRefSave.Get(operand);
                case "dnlib.DotNet.Emit.Local":
                    var temp2 = operand as Local;
                    return new OperandLocal(temp2.Name, TypeRefSave.Get(temp2.Type), temp2.Index);
                case "dnlib.DotNet.Parameter":
                    var temp3 = operand as Parameter;
                    return new OperandParameter(TypeRefSave.Get(temp3.Type), temp3.Index, temp3.MethodSigIndex);
                case "dnlib.DotNet.Emit.Instruction":
                    return OperandInstruction.Get(operand as Instruction, mainContext, secondaryContext);
                case "dnlib.DotNet.Emit.Instruction[]":
                    return OperandInstructionArray.Get(operand as Instruction[], mainContext, secondaryContext);
                default:
                    throw new NotImplementedException(operand.GetType().FullName);
            }
        }
    }

    public interface OperandSave
    {
        public object GetValue(ModuleDefMD module);
    }

    public struct OperandNoOP : OperandSave
    {
        public object GetValue(ModuleDefMD module)
        {
            throw null;
        }
    }

    public struct OperandString : OperandSave
    {
        public OperandString(string Value)
        {
            this.Value = Value;
        }

        public string Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandSByte : OperandSave//Regular byte I think?
    {
        public OperandSByte(sbyte Value)
        {
            this.Value = Value;
        }

        public sbyte Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandFloat32 : OperandSave//Single or float
    {
        public OperandFloat32(float Value)
        {
            this.Value = Value;
        }

        public float Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandFloat64 : OperandSave//Double
    {
        public OperandFloat64(double Value)
        {
            this.Value = Value;
        }

        public double Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandInt32 : OperandSave//Int
    {
        public OperandInt32(int Value)
        {
            this.Value = Value;
        }

        public int Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandInt64 : OperandSave//Long
    {
        public OperandInt64(long Value)
        {
            this.Value = Value;
        }

        public long Value;

        public object GetValue(ModuleDefMD module)
        {
            return Value;
        }
    }

    public struct OperandLocal : OperandSave
    {
        public string Name;
        public TypeRefSave Type;
        public int Index;
        private Local Value;

        public OperandLocal(string Name, TypeRefSave Type, int Index)
        {
            this.Name = Name;
            this.Type = Type;
            this.Index = Index;
            this.Value = null;
        }

        public object GetValue(ModuleDefMD module) => GetValueCast(module);

        public Local GetValueCast(ModuleDefMD module)
        {
            if (Value != null) return Value;
            return Value = new Local(Type.GetValueCast(module).ToTypeSig(), Name, Index);
        }
    }

    public struct OperandInstructionArray : OperandSave
    {
        public OperandInstruction[] Instructions;

        public OperandInstructionArray(OperandInstruction[] Instructions)
        {
            this.Instructions = Instructions;
        }

        public object GetValue(ModuleDefMD module)//Additional processing must be done
        {
            return Instructions;
        }

        public static OperandInstructionArray Get(Instruction[] operand, Instruction[] mainContext, Instruction[] secondaryContext)
        {
            OperandInstruction[] toReturn = new OperandInstruction[operand.Length];

            for(int i = 0;i < operand.Length;i++)
            {
                toReturn[i] = OperandInstruction.Get(operand[i], mainContext, secondaryContext);
            }

            return new OperandInstructionArray(toReturn);
        }
    }

    public struct OperandInstruction : OperandSave
    {
        public OperandInstructionType Type;
        public int Value;

        public OperandInstruction(OperandInstructionType Type, int Value)
        {
            this.Type = Type;
            this.Value = Value;
        }

        public object GetValue(ModuleDefMD module)
        {
            return Value;//DO NOT USE THIS
        }

        public static OperandInstruction Get(Instruction operand, Instruction[] mainContext, Instruction[] secondaryContext)
        {
            for (int i = 0; i < mainContext.Length; i++)
            {
                if (mainContext[i] == operand)
                {
                    return new OperandInstruction(OperandInstructionType.INTERNAL, i);
                }
            }
            if(secondaryContext != null)
            {
                for (int i = 0; i < secondaryContext.Length; i++)
                {
                    if (secondaryContext[i] == operand)
                    {
                        return new OperandInstruction(OperandInstructionType.EXTERNAL, i);
                    }
                }
            }
            throw new ArgumentException();
        }
    }

    public enum OperandInstructionType
    {
        INTERNAL,EXTERNAL//Internal means part of the original method, method means part of the new sections.
    }

    public struct TypeRefSave : OperandSave
    {
        public string Name;
        public string Namespace;
        public string Module;
        public string DefiningAssembly;
        public TypeRefSave[] GenericParameters;
        public int GenericNumber;
        public bool IsTypeVar;//The value of this is undefined if it is not a generic,
        private ITypeDefOrRef Value;

        public TypeRefSave(string Module, string Namespace, string Name, string DefiningAssembly)
        {
            this.Name = Name;
            this.Namespace = Namespace;
            this.Module = Module;
            this.DefiningAssembly = DefiningAssembly;
            this.GenericParameters = new TypeRefSave[0];
            this.GenericNumber = -1;
            this.IsTypeVar = false;
            this.Value = null;
        }

        public TypeRefSave(string Module, string Namespace, string Name, string DefiningAssembly, TypeRefSave[] GenericParameters)
        {
            this.Name = Name;
            this.Namespace = Namespace;
            this.Module = Module;
            this.DefiningAssembly = DefiningAssembly;
            this.GenericParameters = GenericParameters;
            this.GenericNumber = -1;
            this.IsTypeVar = false;
            this.Value = null;
        }

        public TypeRefSave(string Module, string Namespace, string Name, string DefiningAssembly, int GenericNumber, bool IsTypeVar)
        {
            this.Name = Name;
            this.Namespace = Namespace;
            this.Module = Module;
            this.DefiningAssembly = DefiningAssembly;
            this.GenericParameters = new TypeRefSave[0];
            this.GenericNumber = GenericNumber;
            this.IsTypeVar = IsTypeVar;
            this.Value = null;
        }

        public bool IsGenericParameter
        {
            get
            {
                return GenericNumber != -1;
            }
        }

        public object GetValue(ModuleDefMD module) => GetValueCast(module);

        public ITypeDefOrRef GetValueCast(ModuleDefMD module)//TODO: Fixme
        {
            if (Value != null) return Value;
            return Value = SaveUtils.GetType(this, module);
        }

        public static TypeRefSave Get(object operand)
        {
            ITypeDefOrRef typeDef = operand as ITypeDefOrRef;
            if (typeDef != null)
            {
                return GetTypeDef(typeDef);
            }
            TypeSpec typeSpec = operand as TypeSpec;
            if (typeSpec != null)
            {
                return GetTypeSpec(typeSpec);
            }
            TypeSig typeSig = operand as TypeSig;
            if (typeSig != null)
            {
                return GetTypeSig(typeSig);
            }

            throw new NotImplementedException(operand.GetType().FullName);
        }

        public static TypeRefSave GetTypeDef(ITypeDefOrRef typeDef)
        {
            return new TypeRefSave(typeDef.Module.Name, typeDef.Namespace, typeDef.Name, typeDef.DefinitionAssembly.Name);
        }

        public static TypeRefSave GetTypeSpec(TypeSpec typeSpec) => GetTypeDef(typeSpec.ScopeType);//I hope that is correct

        public static TypeRefSave GetTypeSig(TypeSig typeSig)
        {
            string module = "INVALID";//uh no clue here, cant do a ternary conditional for some reason.
            if (typeSig.Module != null) module = typeSig.Module.Name;
            string definitionAssembly = "INVALID";
            if (typeSig.DefinitionAssembly != null) definitionAssembly = typeSig.DefinitionAssembly.Name;
            if (typeSig is GenericInstSig)
            {
                GenericInstSig genericInstSig = typeSig as GenericInstSig;
                TypeRefSave[] genericParameters = new TypeRefSave[genericInstSig.GenericArguments.Count];
                for(int i = 0;i < genericInstSig.GenericArguments.Count;i++)
                {
                    genericParameters[i] = Get(genericInstSig.GenericArguments[i]);
                }
                return new TypeRefSave(module, typeSig.Namespace, typeSig.TypeName, definitionAssembly, genericParameters);
            } 
            else if (typeSig is ArraySigBase)//Array of generics test
            {
                if((typeSig as ArraySigBase).Next is GenericInstSig)//.Next is the element type.
                {
                    GenericInstSig genericInstSig = (typeSig as ArraySigBase).Next as GenericInstSig;//as as as
                    TypeRefSave[] genericParameters = new TypeRefSave[genericInstSig.GenericArguments.Count];//This entire section is a hacky thing to allow arrays of generics to work.
                    for (int i = 0; i < genericInstSig.GenericArguments.Count; i++)
                    {
                        genericParameters[i] = Get(genericInstSig.GenericArguments[i]);
                    }
                    return new TypeRefSave(module, typeSig.Namespace, typeSig.TypeName, definitionAssembly, genericParameters);
                }
            }
            else if (typeSig is GenericVar)
            {
                GenericSig genericSig = typeSig as GenericVar;
                return new TypeRefSave(module, typeSig.Namespace, typeSig.TypeName, definitionAssembly, (int)genericSig.Number, true);//Converting an uint to int so we can have -1 as an invalid value, and who tf has more than 2.147 billion generic parameters anyway?
            }
            else if (typeSig is GenericMVar)
            {
                GenericSig genericSig = typeSig as GenericMVar;
                return new TypeRefSave(module, typeSig.Namespace, typeSig.TypeName, definitionAssembly, (int)genericSig.Number, false);//Converting an uint to int so we can have -1 as an invalid value, and who tf has more than 2.147 billion generic parameters anyway?
            }
            return new TypeRefSave(module, typeSig.Namespace, typeSig.TypeName, definitionAssembly);
        }
    }

    public struct MethodRefSave : OperandSave
    {
        public string Name;
        public string Module;
        public TypeRefSave ReturnSig;
        public TypeRefSave[] ParametersSig;
        public TypeRefSave ContainingType;
        public bool IsStatic;
        private MemberRefUser Value;

        public MethodRefSave(string Module, string Name, TypeRefSave ContainingType, TypeRefSave ReturnSig, TypeRefSave[] ParametersSig, bool IsStatic)
        {
            this.Name = Name;
            this.Module = Module;
            this.ReturnSig = ReturnSig;
            this.ParametersSig = ParametersSig;
            this.ContainingType = ContainingType;
            this.IsStatic = IsStatic;
            this.Value = null;
        }

        public object GetValue(ModuleDefMD module) => GetValueCast(module);

        public IMethod GetValueCast(ModuleDefMD module)
        {
            if (Value != null) return Value;

            foreach (TypeDef type in module.Types)//Attempt #1 - is it a method in this assembly?
            {
                if (type.Name == ContainingType.Name && type.Namespace == ContainingType.Namespace)
                {
                    MethodDef temp = Utils.GetMethodFromType(this, type);
                    if(temp != null)
                    {
                        return temp;
                    }
                }
            }

            //Attempt #2 - Generic MemberRef

            MethodSig sig = null;

            if (IsStatic)
            {
                sig = MethodSig.CreateStatic(ReturnSig.GetValueCast(module).ToTypeSig(), (from p in ParametersSig select p.GetValueCast(module).ToTypeSig()).ToArray());
            } 
            else
            {
                sig = MethodSig.CreateInstance(ReturnSig.GetValueCast(module).ToTypeSig(), (from p in ParametersSig select p.GetValueCast(module).ToTypeSig()).ToArray());
            }

            Value = new MemberRefUser(module, Name, sig, ContainingType.GetValueCast(module));

            return Value;

            //return Value = new MemberRefUser(module, Name, MethodSig.CreateStatic(ReturnSig.GetValueCast(module).ToTypeSig(), (from p in ParametersSig select p.GetValueCast(module).ToTypeSig()).ToArray()), ContainingType.GetValueCast(module));//Im pretty sure this is very wrong
        }

        public static MethodRefSave Get(object operand)
        {
            MemberRef memberRef = operand as MemberRef;
            if (memberRef != null)
            {
                return GetMemberRef(memberRef);
            }
            IMethodDefOrRef methodDef = operand as IMethodDefOrRef;
            if (methodDef != null)
            {
                return GetMethodDef(methodDef);
            }
            MethodSpec methodSpec = operand as MethodSpec;
            if (methodSpec != null)
            {
                return GetMethodSpec(methodSpec);
            }
            throw new NotImplementedException(operand.GetType().FullName);
        }

        private static MethodRefSave GetMemberRef(MemberRef memberRef)
        {
            if (memberRef.IsMethodRef != true)
            {
                throw new ArgumentException();
            }
            MethodSig sig = memberRef.MethodSig;
            return new MethodRefSave(memberRef.Module.Name, memberRef.Name, TypeRefSave.Get(memberRef.DeclaringType), TypeRefSave.Get(sig.RetType), (from t in sig.Params select TypeRefSave.Get(t)).ToArray(), !sig.HasThis);
        }

        private static MethodRefSave GetMethodDef(IMethodDefOrRef methodDef)
        {
            MethodSig sig = methodDef.MethodSig;
            return new MethodRefSave(methodDef.Module.Name, methodDef.Name, TypeRefSave.Get(methodDef.DeclaringType), TypeRefSave.Get(sig.RetType), (from t in sig.Params select TypeRefSave.Get(t)).ToArray(), !sig.HasThis);
        }

        private static MethodRefSave GetMethodSpec(MethodSpec methodSpec) => GetMethodDef(methodSpec.Method);//I hope this is correct
    }

    public struct FieldRefSave : OperandSave
    {
        public string Name;
        public string Module;
        public TypeRefSave Type;
        public TypeRefSave ContainingType;
        private MemberRefUser Value;

        public FieldRefSave(string Module, string Name, TypeRefSave ContainingType, TypeRefSave Type)
        {
            this.Name = Name;
            this.Module = Module;
            this.Type = Type;
            this.ContainingType = ContainingType;
            this.Value = null;
        }

        public object GetValue(ModuleDefMD module) => GetValueCast(module);

        public MemberRefUser GetValueCast(ModuleDefMD module)
        {
            if (Value != null) return Value;
            return Value = new MemberRefUser(module, Name, new FieldSig(Type.GetValueCast(module).ToTypeSig()), ContainingType.GetValueCast(module));
        }

        public static FieldRefSave Get(object operand)
        {
            MemberRef memberRef = operand as MemberRef;
            if (memberRef != null)
            {
                return GetMemberRef(memberRef);
            }
            FieldDef fieldDef = operand as FieldDef;
            if (fieldDef != null)
            {
                return GetFieldDef(fieldDef);
            }
            throw new NotImplementedException(operand.GetType().FullName);
        }

        private static FieldRefSave GetMemberRef(MemberRef memberRef)
        {
            if (memberRef.IsFieldRef != true)
            {
                throw new ArgumentException();
            }
            FieldSig sig = memberRef.FieldSig;
            return new FieldRefSave(memberRef.Module.Name, memberRef.Name, TypeRefSave.Get(memberRef.DeclaringType), TypeRefSave.Get(sig.Type));
        }

        private static FieldRefSave GetFieldDef(FieldDef fieldDef)
        {
            return new FieldRefSave(fieldDef.Module.Name, fieldDef.Name, TypeRefSave.Get(fieldDef.DeclaringType), TypeRefSave.Get(fieldDef.FieldType));
        }
    }

    public struct OperandParameter : OperandSave
    {
        public int ParameterIndex;//Literally no clue what any of this does
        public int MethodSigIndex;
        public TypeRefSave Type;
        private Parameter Value;

        public OperandParameter(TypeRefSave Type, int ParameterIndex, int MethodSigIndex)
        {
            this.Type = Type;
            this.ParameterIndex = ParameterIndex;
            this.MethodSigIndex = MethodSigIndex;
            this.Value = null;
        }

        public object GetValue(ModuleDefMD module) => GetValueCast(module);

        public object GetValueCast(ModuleDefMD module)
        {
            if (Value != null) return Value;
            return Value = new Parameter(ParameterIndex, MethodSigIndex, Type.GetValueCast(module).ToTypeSig());
        }
    }
}
