﻿using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Interaction.PresetSystem.Dialogs
{
    public class AddGroupDialog : ModalDialog
    {
        public void Draw(ref string name)
        {
            DialogSize = new Vector2(500, 280);

            if (BeginDialog("Duplicate as new symbol"))
            {
                // Name and namespace
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.Text("Name of parameter group");
                    ImGui.SameLine();
                    ImGui.PopFont();
                
                    ImGui.SetNextItemWidth(250);
                    if (ImGui.IsWindowAppearing())
                        ImGui.SetKeyboardFocusHere();
                    ImGui.InputText("##groupName", ref name, 255);
                    
                    //CustomComponents.HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                    ImGui.Spacing();
                }
                
                // // Description
                // {
                //     ImGui.PushFont(Fonts.FontSmall);
                //     ImGui.Text("Description");
                //     ImGui.PopFont();
                //     ImGui.SetNextItemWidth(460);
                //     ImGui.InputTextMultiline("##description", ref description, 1024, new Vector2(450, 60));
                // }

                if (CustomComponents.DisablableButton("Duplicate", !string.IsNullOrEmpty( name)))
                {
                    T3Ui.PresetSystem.CreateNewGroupForInput();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }
    }
}