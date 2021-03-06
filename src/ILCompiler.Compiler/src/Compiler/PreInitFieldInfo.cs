﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    public abstract class PreInitFixupInfo : IComparable<PreInitFixupInfo>
    {
        /// <summary>
        /// Offset into the blob
        /// </summary>
        public int Offset { get; }

        public PreInitFixupInfo(int offset)
        {
            Offset = offset;
        }

        int IComparable<PreInitFixupInfo>.CompareTo(PreInitFixupInfo other)
        {
            return this.Offset - other.Offset;
        }

        /// <summary>
        /// Writes fixup data into current ObjectDataBuilder. Caller needs to make sure ObjectDataBuilder is
        /// at correct offset before writing.
        /// </summary>
        public abstract void WriteData(ref ObjectDataBuilder builder, NodeFactory factory);
    }

    public class PreInitTypeFixupInfo : PreInitFixupInfo
    {
        public TypeDesc TypeFixup { get; }

        public PreInitTypeFixupInfo(int offset, TypeDesc type)
            :base(offset)
        {
            TypeFixup = type;
        }

        public override void WriteData(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            builder.EmitPointerReloc(factory.NecessaryTypeSymbol(TypeFixup));
        }
    }

    public class PreInitMethodFixupInfo : PreInitFixupInfo
    {
        public MethodDesc MethodFixup { get; }

        public PreInitMethodFixupInfo(int offset, MethodDesc method)
            : base(offset)
        {
            if (method.HasInstantiation || method.OwningType.HasInstantiation)
                throw new BadImageFormatException();

            MethodFixup = method;
        }

        public override void WriteData(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            builder.EmitPointerReloc(factory.MethodEntrypoint(MethodFixup));
        }
    }

    public class PreInitFieldInfo
    {
        public FieldDesc Field { get; }

        /// <summary>
        /// Points to the underlying contents of the data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Number of elements, if this is a frozen array.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// List of fixup to be apply to the data blob
        /// This is needed for information that can't be encoded into blob ahead of time before codegen
        /// </summary>
        private List<PreInitFixupInfo> FixupInfos;

        public PreInitFieldInfo(FieldDesc field, byte[] data, int length, List<PreInitFixupInfo> fixups)
        {
            Field = field;
            Data = data;
            Length = length;
            FixupInfos = fixups;

            if (FixupInfos != null)
                FixupInfos.Sort();
        }

        public void WriteData(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            int offset = 0;

            if (FixupInfos != null)
            {
                int startOffset = builder.CountBytes;

                for (int i = 0; i < FixupInfos.Count; ++i)
                {
                    var fixupInfo = FixupInfos[i];

                    // do we have overlapping fixups?
                    if (fixupInfo.Offset < offset)
                        throw new BadImageFormatException();

                    // emit bytes before fixup
                    builder.EmitBytes(Data, offset, fixupInfo.Offset - offset);

                    // write the fixup
                    FixupInfos[i].WriteData(ref builder, factory);

                    // move pointer past the fixup
                    offset = builder.CountBytes - startOffset;
                }
            }

            if (offset > Data.Length)
                throw new BadImageFormatException();
            
            // Emit remaining bytes
            builder.EmitBytes(Data, offset, Data.Length - offset);
        }

        public static List<PreInitFieldInfo> GetPreInitFieldInfos(TypeDesc type)
        {
            List<PreInitFieldInfo> list = null;

            foreach (var field in type.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                var dataField = GetPreInitDataField(field);
                if (dataField != null)
                {
                    if (list == null)
                        list = new List<PreInitFieldInfo>();
                    list.Add(ConstructPreInitFieldInfo(field, dataField));
                }
            }

            return list;
        }

        /// <summary>
        /// Retrieves the corresponding static preinitialized data field by looking at various attributes
        /// </summary>
        private static FieldDesc GetPreInitDataField(FieldDesc thisField)
        {
            Debug.Assert(thisField.IsStatic);

            var field = thisField as EcmaField;
            if (field == null)
                return null;

            if (!field.HasCustomAttribute("System.Runtime.CompilerServices", "PreInitializedAttribute"))
                return null;

            var decoded = field.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "InitDataBlobAttribute");
            if (decoded == null)
                return null; 

            var decodedValue = decoded.Value;
            if (decodedValue.FixedArguments.Length != 2)
                throw new BadImageFormatException();

            var typeDesc = decodedValue.FixedArguments[0].Value as TypeDesc;
            if (typeDesc == null)
                throw new BadImageFormatException(); 

            if (decodedValue.FixedArguments[1].Type != field.Context.GetWellKnownType(WellKnownType.String))
                throw new BadImageFormatException(); 

            var fieldName = (string)decodedValue.FixedArguments[1].Value;
            var dataField = typeDesc.GetField(fieldName);
            if (dataField== null)
                throw new BadImageFormatException();

            return dataField;
        }

        /// <summary>
        /// Extract preinitialize data as byte[] from a RVA field, and perform necessary validations.
        /// </summary>
        private static PreInitFieldInfo ConstructPreInitFieldInfo(FieldDesc field, FieldDesc dataField)
        {
            var arrType = field.FieldType as ArrayType;
            if (arrType == null || !arrType.IsSzArray)
            {
                // We only support single dimensional arrays
                throw new NotSupportedException();
            }

            if (!dataField.HasRva)
                throw new BadImageFormatException();
            
            var ecmaDataField = dataField as EcmaField;
            if (ecmaDataField == null)
                throw new NotSupportedException();
            
            var rvaData = ecmaDataField.GetFieldRvaData();

            int elementSize = arrType.ElementType.GetElementSize().AsInt;
            if (rvaData.Length % elementSize != 0)
                throw new BadImageFormatException();
            int elementCount = rvaData.Length / elementSize;

            //
            // Construct fixups
            //
            List<PreInitFixupInfo> fixups = null;            

            var typeFixupAttrs = ecmaDataField.GetDecodedCustomAttributes("System.Runtime.CompilerServices", "TypeHandleFixupAttribute");
            foreach (var typeFixupAttr in typeFixupAttrs)
            {
                if (typeFixupAttr.FixedArguments[0].Type != field.Context.GetWellKnownType(WellKnownType.Int32))
                    throw new BadImageFormatException();

                int offset = (int)typeFixupAttr.FixedArguments[0].Value;
                TypeDesc fixupType = typeFixupAttr.FixedArguments[1].Value as TypeDesc;
                if (fixupType == null)
                    throw new BadImageFormatException();

                fixups = fixups ?? new List<PreInitFixupInfo>();

                fixups.Add(new PreInitTypeFixupInfo(offset, fixupType));
            }

            var methodFixupAttrs = ecmaDataField.GetDecodedCustomAttributes("System.Runtime.CompilerServices", "MethodAddrFixupAttribute");
            foreach (var methodFixupAttr in methodFixupAttrs)
            {
                if (methodFixupAttr.FixedArguments[0].Type != field.Context.GetWellKnownType(WellKnownType.Int32))
                    throw new BadImageFormatException();

                int offset = (int)methodFixupAttr.FixedArguments[0].Value;
                TypeDesc fixupType = methodFixupAttr.FixedArguments[1].Value as TypeDesc;
                if (fixupType == null)
                    throw new BadImageFormatException();

                string methodName = methodFixupAttr.FixedArguments[2].Value as string;
                if (methodName == null)
                    throw new BadImageFormatException();

                var method = fixupType.GetMethod(methodName, signature : null);
                if (method == null)
                    throw new BadImageFormatException();

                fixups = fixups ?? new List<PreInitFixupInfo>();

                fixups.Add(new PreInitMethodFixupInfo(offset, method));
            }

            return new PreInitFieldInfo(field, rvaData, elementCount, fixups);
        }
    }
}
