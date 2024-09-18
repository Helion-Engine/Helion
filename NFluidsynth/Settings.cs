using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class Settings : FluidsynthObject
    {
        public Settings()
            : base(LibFluidsynth.new_fluid_settings())
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_settings(Handle);
            }

            base.Dispose(disposing);
        }

        public SettingEntry this[string name]
        {
            get
            {
                ThrowIfDisposed();
                return new SettingEntry(this, name);
            }
        }

        public sealed class SettingEntry
        {
            private readonly Settings _parent;
            private readonly string _name;

            internal SettingEntry(Settings parent, string name)
            {
                _parent = parent;
                _name = name;
            }

            public string Name => _name;

            public FluidTypes Type
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    return LibFluidsynth.fluid_settings_get_type(_parent.Handle, _name);
                }
            }

            public FluidHint Hints
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(
                        LibFluidsynth.fluid_settings_get_hints(_parent.Handle, _name, out var hints));
                    return hints;
                }
            }

            public unsafe string StringDefault
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(
                        LibFluidsynth.fluid_settings_getstr_default(_parent.Handle, _name, out var ret));

                    return Utility.PtrToStringUTF8(ret);
                }
            }

            public unsafe string StringValue
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_dupstr(_parent.Handle, _name, out var ret));

                    return Utility.PtrToStringUTF8(ret);
                }
                set
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_setstr(_parent.Handle, _name, value));
                }
            }

            public int IntDefault
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(
                        LibFluidsynth.fluid_settings_getint_default(_parent.Handle, _name, out var ret));
                    return ret;
                }
            }

            public (int min, int max) IntRange
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(
                        LibFluidsynth.fluid_settings_getint_range(_parent.Handle, _name, out var min, out var max));
                    return (min, max);
                }
            }

            public int IntValue
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_getint(_parent.Handle, _name, out var v));
                    return v;
                }
                set
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_setint(_parent.Handle, _name, value));
                }
            }

            public double DoubleDefault
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(
                        LibFluidsynth.fluid_settings_getnum_default(_parent.Handle, _name, out var ret));
                    return ret;
                }
            }

            public (double min, double max) DoubleRange
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_getnum_range(_parent.Handle, _name,
                        out var min, out var max));
                    return (min, max);
                }
            }

            public double DoubleValue
            {
                get
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_getnum(_parent.Handle, _name, out var ret));
                    return ret;
                }
                set
                {
                    _parent.ThrowIfDisposed();
                    Utility.CheckReturnValue(LibFluidsynth.fluid_settings_setnum(_parent.Handle, _name, value));
                }
            }

            public void ForeachOption(Action<string, string> func)
            {
                throw new NotImplementedException();
                /*
                LibFluidsynth.fluid_settings_foreach_option_t f = (d, nm, opt) =>
                {
                    func(nm, opt);
                    return IntPtr.Zero;
                };
                LibFluidsynth.fluid_settings_foreach_option(_parent.Handle, _name, IntPtr.Zero, f);
                */
            }

            public void ForeachOption(Func<IntPtr, string, string, IntPtr> func, IntPtr data = default(IntPtr))
            {
                throw new NotImplementedException();
                /*
                LibFluidsynth.fluid_settings_foreach_option_t f = (d, nm, opt) => func(d, nm, opt);
                LibFluidsynth.fluid_settings_foreach_option(_parent.Handle, _name, data, f);
                */
            }

            public void Foreach(Action<string, FluidTypes> func)
            {
                throw new NotImplementedException();
                /*
                LibFluidsynth.fluid_settings_foreach_t f = (d, nm, t) =>
                {
                    func(nm, t);
                    return IntPtr.Zero;
                };
                LibFluidsynth.fluid_settings_foreach(_handle, _name, IntPtr.Zero, f);
                */
            }

            public void Foreach(Func<IntPtr, string, FluidTypes, IntPtr> func, IntPtr data = default(IntPtr))
            {
                throw new NotImplementedException();
                /*
                LibFluidsynth.fluid_settings_foreach_t f = (d, nm, t) => func(d, nm, t);
                LibFluidsynth.fluid_settings_foreach(_handle, _name, data, f);
                */
            }
        }
    }
}