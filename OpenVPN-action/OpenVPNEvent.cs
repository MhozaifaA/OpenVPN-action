using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVPN_action
{
    /// <summary>
    /// custom name and event with <see cref="{TArgs}"/> useful OpenVPN 
    /// </summary>
    /// <typeparam name="TArgs"></typeparam>
    /// <param name="e"></param>
    public delegate void OpenVPNEvent<TArgs>(TArgs e);
}
