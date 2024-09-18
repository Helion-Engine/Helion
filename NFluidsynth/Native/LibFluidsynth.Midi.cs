using System;
using System.Runtime.InteropServices;
using fluid_midi_event_t_ptr = System.IntPtr;
using fluid_midi_router_t_ptr = System.IntPtr;
using fluid_player_t_ptr = System.IntPtr;
using fluid_settings_t_ptr = System.IntPtr;
using fluid_midi_router_rule_t_ptr = System.IntPtr;
using fluid_midi_driver_t_ptr = System.IntPtr;
using fluid_synth_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    internal static unsafe partial class LibFluidsynth
    {

#if NETCOREAPP
        const UnmanagedType LP_Str = System.Runtime.InteropServices.UnmanagedType.LPUTF8Str;
#else
        const UnmanagedType LP_Str = System.Runtime.InteropServices.UnmanagedType.LPStr;
#endif
        internal delegate int handle_midi_event_func_t(void* data, fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern fluid_midi_event_t_ptr new_fluid_midi_event();

        [DllImport(LibraryName)]
        internal static extern int delete_fluid_midi_event(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_type(fluid_midi_event_t_ptr evt,
            int type);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_type(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_channel(fluid_midi_event_t_ptr evt,
            int chan);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_channel(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_key(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_key(fluid_midi_event_t_ptr evt,
            int key);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_velocity(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_velocity(fluid_midi_event_t_ptr evt,
            int vel);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_control(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_control(fluid_midi_event_t_ptr evt,
            int ctrl);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_value(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_value(fluid_midi_event_t_ptr evt,
            int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_program(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_program(fluid_midi_event_t_ptr evt,
            int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_get_pitch(fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_pitch(fluid_midi_event_t_ptr evt,
            int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_event_set_sysex(fluid_midi_event_t_ptr evt, void* data,
            int size, bool isDynamic);

        [DllImport(LibraryName)]
        internal static extern fluid_midi_router_t_ptr new_fluid_midi_router(fluid_settings_t_ptr settings,
            IntPtr handler, void* event_handler_data);

        [DllImport(LibraryName)]
        internal static extern int delete_fluid_midi_router(fluid_midi_router_t_ptr handler);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_router_set_default_rules(fluid_midi_router_t_ptr router);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_router_clear_rules(fluid_midi_router_t_ptr router);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_router_add_rule(fluid_midi_router_t_ptr router,
            fluid_midi_router_rule_t_ptr rule, FluidMidiRouterRuleType type);

        [DllImport(LibraryName)]
        internal static extern fluid_midi_router_rule_t_ptr new_fluid_midi_router_rule();

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_midi_router_rule(fluid_midi_router_rule_t_ptr rule);

        [DllImport(LibraryName)]
        internal static extern void fluid_midi_router_rule_set_chan(fluid_midi_router_rule_t_ptr rule,
            int min, int max, float mul,
            int add);

        [DllImport(LibraryName)]
        internal static extern void fluid_midi_router_rule_set_param1(fluid_midi_router_rule_t_ptr rule,
            int min, int max, float mul,
            int add);

        [DllImport(LibraryName)]
        internal static extern void fluid_midi_router_rule_set_param2(fluid_midi_router_rule_t_ptr rule,
            int min, int max, float mul,
            int add);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_router_handle_midi_event(void* data, fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_dump_prerouter(void* data, fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_midi_dump_postrouter(void* data, fluid_midi_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern fluid_midi_driver_t_ptr new_fluid_midi_driver(fluid_settings_t_ptr settings,
            IntPtr handler, void* event_handler_data);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_midi_driver(fluid_midi_driver_t_ptr driver);

        [DllImport(LibraryName)]
        internal static extern fluid_player_t_ptr new_fluid_player(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int delete_fluid_player(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_add(fluid_player_t_ptr player,
            [MarshalAs(LP_Str)] string midifile);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_add_mem(fluid_player_t_ptr player, IntPtr buffer, int size);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_play(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_stop(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_join(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_set_loop(fluid_player_t_ptr player,
            int loop);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_set_midi_tempo(fluid_player_t_ptr player,
            int tempo);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_set_bpm(fluid_player_t_ptr player,
            int bpm);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_set_playback_callback(fluid_player_t_ptr player,
            IntPtr handler, void* handlerData);

        [DllImport(LibraryName)]
        internal static extern FluidPlayerStatus fluid_player_get_status(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_get_current_tick(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_get_total_ticks(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_get_bpm(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_get_midi_tempo(fluid_player_t_ptr player);

        [DllImport(LibraryName)]
        internal static extern int fluid_player_seek(fluid_player_t_ptr player, int ticks);
    }
}