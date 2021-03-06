﻿// Copyright © 2013 Oracle and/or its affiliates. All rights reserved.
//
// MySQL for Visual Studio is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Installer.Tests
{
  public class RegressionTests : IUseFixture<SetUpClass>, IDisposable
  {
    private SetUpClass st;
    private bool disposed = false;

    public void SetFixture(SetUpClass data)
    {
      st = data;
    }

    /// <summary>
    /// Checks that the UpdateMachineConfig custom action does not exist in the installer.
    /// </summary>
    [Fact]
    public void UpdateMachineConfigAction()
    {
      string val;
      // The UpdateMachineConfigFile action must not exists neither in table CustomAction nor table InstallExecuteSequence
      st.GetValue("select `Action` from `CustomAction` where `Action` = 'UpdateMachineConfigFile'", out val);
      Assert.Equal(val, null /*"UpdateMachineConfigFile" */);
      st.GetValue("select `Action` from `InstallExecuteSequence` where `Action` = 'UpdateMachineConfigFile'", out val);
      Assert.Equal(val, null /*"UpdateMachineConfigFile" */);
    }

    [Fact]
    public void NoInstallUtilCustomAction()
    {
      string val;
      st.GetValue("select `Action` from `CustomAction` where `Action` = 'ManagedDataInstallSetup'", out val);
      Assert.Equal(val, null);
      st.GetValue("select `Action` from `CustomAction` where `Action` = 'ManagedDataUnInstallSetup'", out val);
      Assert.Equal(val, null);
    }

    public virtual void Dispose()
    {
    }
  }
}
