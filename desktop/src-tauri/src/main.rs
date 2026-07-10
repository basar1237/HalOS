// Windows'ta prodüksiyon derlemesinde konsol penceresini gizle.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    halos_terminal_lib::run()
}
