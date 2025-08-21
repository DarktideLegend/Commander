using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter.Wrappers;
using Decal.Interop.Core;

namespace Commander.Lib.Services
{
    public interface BlinkService
    {
        void BlinkObject(WorldObject blinkObject);
        void BlinkTheWorld();
        void Init();
    }

    public class BlinkServiceImpl: BlinkService
    {
        private bool _disposed = false;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate float f(IntPtr A_0);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void goo(int A_0, int A_1);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int moo(IntPtr A_0, [MarshalAs(UnmanagedType.BStr)] string A_1, ref int A_2);

        private long lastBlinkTime = 0;

        private static List<string> AllowedItems = new List<string>
        {
            "Pile of Rocks",
            "Pile of Stones",
        };

        private Logger _logger;
        private SettingsManager _settingsManager;
        private GlobalProvider _globals;

        public BlinkServiceImpl(
            Logger logger,
            SettingsManager settingsManager,
            GlobalProvider globals)
        {
            _logger = logger.Scope("BlinkService");
            _settingsManager = settingsManager;
            _globals = globals;
        }

        public void Init()
        {
            _globals.Core.CharacterFilter.Login += CharacterFilter_Login;
            _globals.Core.CharacterFilter.Logoff += CharacterFilter_Logoff;
            _globals.Core.RenderFrame += Core_RenderFrame;
            _logger.Info("BlinkService initialized.");
        }


        public void BlinkObject(WorldObject blinkObject)
        {
            try
            {
                if (_globals.Core.WorldFilter[blinkObject.Id] == null)
                {
                    return;
                }
                try
                {
                    e(blinkObject.Id);
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public void BlinkTheWorld()
        {
            if (!_settingsManager.Settings.Blink)
                return;

            foreach (WorldObject item in _globals.Core.WorldFilter.GetLandscape())
            {
                if (checkAllowedItem(item))
                {
                    BlinkObject(item);
                }
            }
        }

        public bool checkAllowedItem(WorldObject blinkItem)
        {
            var objClass = _globals.Core.WorldFilter[blinkItem.Id].ObjectClass;

            if (objClass == ObjectClass.Door)
                return true;

            if (_settingsManager.Settings.BlinkMobs && objClass == ObjectClass.Monster)
                return true;

            if (AllowedItems.Contains(blinkItem.Name))
                return true;

            return false;
        }

        public float m(int A_0)
        {
            if (!_globals.Host.Actions.IsValidObject(A_0))
            {
                return 0f;
            }
            IntPtr intPtr = _globals.Host.Actions.PhysicsObject(A_0);
            if (intPtr == IntPtr.Zero)
            {
                return 0f;
            }
            int num = h("9c5e52a21260eb9f53be0ecf39c6e17e");
            return (num != 0) ? ((f)Marshal.GetDelegateForFunctionPointer((IntPtr)num, typeof(f)))(intPtr) : 0f;
        }

        internal unsafe void e(int worldObjectId)
        {
            try
            {
                if (_globals.Host.Actions.IsValidObject(worldObjectId))
                {
                    //Util.WriteToChat($"Blinking item with Id = {worldObjectId}");
                    ((goo)Marshal.GetDelegateForFunctionPointer((IntPtr)h("56188e71599e0d3a73146e53cd01b972"), typeof(goo)))(*(int*)h("395046a14d79ae85df4653af740b0189"), worldObjectId);
                }
            }
            catch
            {
            }
        }

        private ACHooks c()
        {
            return _globals.Host.Underlying.Hooks;
        }

        internal int h(string A_0)
        {
            IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(c());
            if (iUnknownForObject == IntPtr.Zero)
            {
                return 0;
            }
            try
            {
                IntPtr ppv = IntPtr.Zero;
                Guid iid = new Guid("67AFC283-4A3C-4d5d-86C0-3EA230687FFE");
                Marshal.QueryInterface(iUnknownForObject, ref iid, out ppv);
                if (ppv == IntPtr.Zero)
                {
                    return 0;
                }
                try
                {
                    IntPtr intPtr = a(ppv, 0);
                    if (intPtr == IntPtr.Zero)
                    {
                        return 0;
                    }
                    moo moo = (moo)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(moo));
                    int A_1 = 0;
                    IntPtr a_ = ppv;
                    int num = moo(a_, A_0, ref A_1);
                    return A_1;
                }
                finally
                {
                    Marshal.Release(ppv);
                }
            }
            finally
            {
                Marshal.Release(iUnknownForObject);
            }
        }

        private unsafe IntPtr a(IntPtr A_0, int A_1)
        {
            try
            {
                if (A_0 == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }
                int* ptr = (int*)(int)(*(uint*)(void*)A_0);
                if ((IntPtr)ptr == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }
                int* ptr2 = (int*)((int)ptr + A_1 * 4 + 12);
                if ((IntPtr)ptr2 == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }
                int* ptr3 = (int*)(*ptr2);
                return ((IntPtr)ptr3 == IntPtr.Zero) ? IntPtr.Zero : ((IntPtr)ptr3);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private void Core_RenderFrame(object sender, EventArgs e)
        {
            try
            {

                var blinkInterval = _settingsManager.Settings.BlinkInterval;
                long currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                if (currentTime - lastBlinkTime >= blinkInterval)
                {
                    BlinkTheWorld();
                    lastBlinkTime = currentTime;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

        }

        private void CharacterFilter_Login(object sender, LoginEventArgs e)
        {
            try
            {
                var blinkInterval = _settingsManager.Settings.BlinkInterval;
                lastBlinkTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                _logger.Info($"Custom blink timer started with a duration of {blinkInterval / 1000} seconds");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

        }

        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                lastBlinkTime = 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _globals.Core.CharacterFilter.Login -= CharacterFilter_Login;
                    _globals.Core.CharacterFilter.Logoff -= CharacterFilter_Logoff;
                    _globals.Core.RenderFrame -= Core_RenderFrame;
                }
                _disposed = true;
            }
        }
    }
}
