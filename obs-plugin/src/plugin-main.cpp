// SPDX-License-Identifier: GPL-2.0-or-later
#include <obs-module.h>
#include <obs-frontend-api.h>
#include <QPointer>
#include "panel.hpp"

OBS_DECLARE_MODULE()
OBS_MODULE_USE_DEFAULT_LOCALE("casterdesk-obs", "en-US")
MODULE_EXPORT const char *obs_module_description(void) { return "CasterDesk timer and operator bans dock"; }
MODULE_EXPORT const char *obs_module_name(void) { return "CasterDesk"; }
static QPointer<CasterDeskPanel> panel;
static bool exiting = false;
static void eventCallback(enum obs_frontend_event event, void *)
{
    if (event == OBS_FRONTEND_EVENT_FINISHED_LOADING && !panel) {
        auto *widget = new CasterDeskPanel();
        if (obs_frontend_add_dock_by_id("casterdesk-control", "CasterDesk", widget)) panel = widget;
        else delete widget;
    }
    if (event == OBS_FRONTEND_EVENT_EXIT) exiting = true;
}
bool obs_module_load(void)
{
    exiting = false;
    obs_frontend_add_event_callback(eventCallback, nullptr);
    return true;
}
void obs_module_unload(void)
{
    if (!exiting) {
        obs_frontend_remove_event_callback(eventCallback, nullptr);
        if (panel) obs_frontend_remove_dock("casterdesk-control");
    }
    panel = nullptr;
}
