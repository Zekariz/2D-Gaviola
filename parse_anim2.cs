using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string text = File.ReadAllText("Assets/Animations/Player/Player.controller");
        
        var stateMatches = Regex.Matches(text, @"--- !u!1102 &(-?\d+)\s+AnimatorState:[\s\S]*?m_Name: (.*?)\n");
        var names = new System.Collections.Generic.Dictionary<string, string>();
        foreach (Match m in stateMatches) {
            names[m.Groups[1].Value] = m.Groups[2].Value.Trim();
        }

        var transMatches = Regex.Matches(text, @"--- !u!1101 &(-?\d+)[\s\S]*?AnimatorStateTransition:[\s\S]*?m_Conditions:\s*([\s\S]*?)\s*m_DstStateMachine:[\s\S]*?m_DstState: \{fileID: (-?\d+)\}[\s\S]*?m_ExitTime: ([\d\.]+)[\s\S]*?m_HasExitTime: (\d)");
        var transitions = new System.Collections.Generic.Dictionary<string, string>();
        foreach (Match m in transMatches) {
            string id = m.Groups[1].Value;
            string dst = m.Groups[3].Value;
            string exitTime = m.Groups[4].Value;
            string hasExit = m.Groups[5].Value;
            
            string dstName = names.ContainsKey(dst) ? names[dst] : dst;
            
            var conds = new System.Collections.Generic.List<string>();
            var condMatches = Regex.Matches(m.Groups[2].Value, @"m_ConditionMode: (\d+)[\s\S]*?m_ConditionEvent: (.*?)\s+m_EventTreshold: ([\d\.]+)");
            foreach (Match cm in condMatches) {
                string mode = cm.Groups[1].Value;
                string evt = cm.Groups[2].Value;
                string thresh = cm.Groups[3].Value;
                string mStr = mode == "1" ? "If" : mode == "2" ? "IfNot" : mode == "3" ? "Greater" : mode == "4" ? "Less" : mode;
                conds.Add(mStr + "(" + evt + ")");
            }
            
            transitions[id] = string.Format("-> {0} | hasExit={1} | Conds: {2}", dstName, hasExit, string.Join(", ", conds));
        }

        var stateFull = Regex.Matches(text, @"--- !u!1102 &(-?\d+)[\s\S]*?m_Name: (.*?)[\s\S]*?m_Transitions:\s*([\s\S]*?)\s*m_StateMachineBehaviours:");
        foreach (Match m in stateFull) {
            string sName = m.Groups[2].Value.Trim();
            Console.WriteLine("\nSTATE: " + sName);
            var tMatches = Regex.Matches(m.Groups[3].Value, @"\{fileID: (-?\d+)\}");
            foreach (Match tm in tMatches) {
                string tid = tm.Groups[1].Value;
                if (transitions.ContainsKey(tid)) {
                    Console.WriteLine("  " + transitions[tid]);
                }
            }
        }
    }
}
