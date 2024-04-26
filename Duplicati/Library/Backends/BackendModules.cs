// Copyright (C) 2024, The Duplicati Team
// https://duplicati.com, hello@duplicati.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Duplicati.Library.Interface;

namespace Duplicati.Library.Backends;

public static class BackendModules
{
    /// <summary>
    /// The list of all built-in backend modules
    /// </summary>
    public static IReadOnlyList<IBackend> BuiltInBackendModules => [
        new Backend.AliyunOSS.OSS(),
        new Backend.AlternativeFTP.AlternativeFtpBackend(),
        new Backend.AzureBlob.AzureBlobBackend(),
        new Backend.Backblaze.B2(),
        new Backend.Box.BoxBackend(),
        new Backend.CloudFiles(),
        new Backend.Dropbox(),
        new Backend.FTP(),
        new Backend.File(),
        new Backend.GoogleCloudStorage.GoogleCloudStorage(),
        new Backend.GoogleDrive.GoogleDrive(),
        new Backend.Idrivee2Backend(),
        new Backend.Jottacloud(),
        new Backend.Mega.MegaBackend(),
        new Backend.MicrosoftGroup(),
        new Backend.OneDriveV2(),
        new Backend.OpenStack.OpenStackStorage(),
        new Backend.Rclone(),
        new Backend.S3(),
        new Backend.SSHv2(),
        new Backend.OneDriveForBusinessBackend(),
        new Backend.SharePointBackend(),
        new Backend.SharePointV2(),
        new Backend.Sia.Sia(),
        new Backend.Storj.Storj(),
        new Backend.TahoeBackend(),
        new Backend.TencentCOS.COS(),
        new Backend.WEBDAV()
    ];
}
