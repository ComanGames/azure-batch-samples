.. image:: https://travis-ci.org/alfpark/azure-batch-samples.svg?branch=master
  :target: https://travis-ci.org/alfpark/azure-batch-samples
.. image:: https://coveralls.io/repos/alfpark/azure-batch-samples/badge.svg?branch=master&service=github
  :target: https://coveralls.io/github/alfpark/azure-batch-samples?branch=master
.. image:: https://img.shields.io/pypi/v/blobxfer.svg
  :target: https://pypi.python.org/pypi/blobxfer
.. image:: https://img.shields.io/pypi/dm/blobxfer.svg
  :target: https://pypi.python.org/pypi/blobxfer
.. image:: https://img.shields.io/pypi/pyversions/blobxfer.svg
  :target: https://pypi.python.org/pypi/blobxfer
.. image:: https://img.shields.io/pypi/l/blobxfer.svg
  :target: https://pypi.python.org/pypi/blobxfer

blobxfer
========
AzCopy-like OS independent Azure storage blob transfer tool

Installation
------------
blobxfer is on PyPI and can be installed via:

::

  pip install blobxfer

If you encounter difficulties installing the script, it may be due to the
``cryptography`` dependency. Please ensure that your system is able to install
binary wheels provided by these dependencies (e.g., on Windows) or is able to
compile the dependencies (i.e., ensure you have a C compiler, python, ssl,
and ffi development libraries/headers installed prior to invoking pip).

If you need more fine-grained control on installing dependencies, continue
reading this section. The blobxfer utility is a python script that can be used
on any platform where Python 2.7, 3.3, 3.4 or 3.5 is installable. Depending
upon the desired mode of authentication with Azure and options, the script
will require the following packages, some of which will automatically pull
required dependent packages. Below is a list of required packages:

- Base Requirements

  - ``azure-common`` == 1.1.1
  - ``azure-storage`` == 0.30.0
  - ``requests`` == 2.9.1

- Encryption Support

  - ``cryptography`` >= 1.3

- Service Management Certificate Support

  - ``azure-servicemanagement-legacy`` == 0.20.2

You can install these packages using pip, easy_install or through standard
setup.py procedures. These dependencies will be automatically installed if
using a package-based install or setup.py.

Introduction
------------

The blobxfer.py script allows interacting with storage accounts using any of
the following methods: (1) management certificate, (2) shared account key,
(3) SAS key. The script can, in addition to working with single files, mirror
entire directories into and out of containers from Azure Storage, respectively.
Block- and file-level MD5 checksumming for data integrity is supported along
with various transfer optimizations, built-in retries, and user-specified
timeouts.

Program parameters and command-line options can be listed via the ``-h``
switch. Please invoke this first if you are unfamiliar with blobxfer operation
as not all options are explained below. At the minimum, three positional
arguments are required: storage account name, container name, local resource.
Additionally, one of the following authentication switches must be supplied:
``--subscriptionid`` with ``--managementcert``, ``--storageaccountkey``,
or ``--saskey``. Do not combine different authentication schemes together. It
is generally recommended to use SAS keys wherever possible; only HTTPS
transport is used in the script.

Please remember when using SAS keys that only container-level SAS keys will
allow for entire directory uploading or container downloading. The container
must also have been created beforehand, as containers cannot be created
using SAS keys.

Please refer to the Microsoft HPC and Azure Batch Team `blog post`_ for code
sample explanations.

.. _blog post: http://blogs.technet.com/b/windowshpc/archive/2015/04/16/linux-blob-transfer-python-code-sample.aspx

Example Usage
-------------

The following examples show how to invoke the script with commonly used
options. Note that the authentication parameters are missing from the below
examples. You will need to select a preferred method of authenticating with
Azure and add the authentication switches as noted above.

The script will attempt to perform a smart transfer, by detecting if the local
resource exists. For example:

::

  blobxfer mystorageacct container0 mylocalfile.txt

Note: if you downloaded the script directly from github, then you should append
``.py`` to the blobxfer command.

If mylocalfile.txt exists locally, then the script will attempt to upload the
file to container0 on mystorageacct. If the file does not exist, then it will
attempt to download the resource. If the desired behavior is to download the
file from Azure even if the local file exists, one can override the detection
mechanism with ``--download``. ``--upload`` is available to force the transfer
to Azure storage. Note that specifying a particular direction does not force
the actual operation to occur as that depends on other options specified such
as skipping on MD5 matches. Note that you may use the ``--remoteresource`` flag
to rename the local file as the blob name on Azure storage if uploading,
however, ``--remoteresource`` has no effect if uploading a directory of files.
Please refer to the ``--collate`` option as explained below.

If the local resource is a directory that exists, the script will attempt to
mirror (recursively copy) the entire directory to Azure storage while
maintaining subdirectories as virtual directories in Azure storage. You can
disable the recursive copy (i.e., upload only the files in the directory)
using the ``--no-recursive`` flag.

To upload a directory with files only matching a Unix-style shell wildcard
pattern, an example commandline would be:

::

  blobxfer mystorageacct container0 mylocaldir --upload --include '**/*.txt'

This would attempt to recursively upload the contents of mylocaldir
to container0 for any file matching the wildcard pattern ``*.txt`` within
all subdirectories. Include patterns can be applied for uploads as well as
downloads. Note that you will need to prevent globbing by your shell such
that wildcard expansion does not take place before script interprets the
argument.

To download an entire container from your storage account, an example
commandline would be:

::

  blobxfer mystorageacct container0 mylocaldir --remoteresource .

Assuming mylocaldir directory does not exist, the script will attempt to
download all of the contents in container0 because “.” is set with
``--remoteresource`` flag. To download individual blobs, one would specify the
blob name instead of “.” with the ``--remoteresource`` flag. If mylocaldir
directory exists, the script will attempt to upload the directory instead of
downloading it. In this case, if you want to force the download direction,
indicate that with ``--download``. When downloading an entire container, the
script will attempt to pre-allocate file space and recreate the sub-directory
structure as needed.

To collate files into specified virtual directories or local paths, use
the ``--collate`` flag with the appropriate parameter. For example, the
following commandline:

::

  blobxfer mystorageacct container0 myvhds --upload --collate vhds --autovhd

If the directory ``myvhds`` had two vhd files a.vhd and subdir/b.vhd, these
files would be uploaded into ``container0`` under the virtual directory named
``vhds``, and b.vhd would not contain the virtual directory subdir; thus,
flattening the directory structure. The ``--autovhd`` flag would automatically
enable page blob uploads for these files. If you wish to collate all files
into the container directly, you would replace ``--collate vhds`` with
``--collate .``

To strip leading components of a path on upload, use ``--strip-components``
with a number argument which will act similarly to tar's
``--strip-components=NUMBER`` parameter. This parameter is only applied
during an upload.

To encrypt or decrypt files, the option ``--rsapublickey`` and
``--rsaprivatekey`` is available. This option requires a file location for a
PEM encoded RSA public or private key. An optional parameter,
``--rsakeypassphrase`` is available for passphrase protected RSA private keys.

To encrypt and upload, only the RSA public key is required although an RSA
private key may be specified. To download and decrypt blobs which are
encrypted, the RSA private key is required.

::

  blobxfer mystorageacct container0 myblobs --upload --rsapublickey mypublickey.pem

The above example commandline would encrypt and upload files contained in
``myblobs`` using an RSA public key named ``mypublickey.pem``. An RSA private
key may be specified instead for uploading (public parts will be used).

::

  blobxfer mystorageacct container0 myblobs --remoteresouorce . --download --rsaprivatekey myprivatekey.pem

The above example commandline would download and decrypt all blobs in the
container ``container0`` using an RSA private key named ``myprivatekey.pem``.
An RSA private key must be specified for downloading and decryption of
encrypted blobs.

Currently only the ``FullBlob`` encryption mode is supported for the
parameter ``--encmode``. The ``FullBlob`` encryption mode either uploads or
downloads Azure Storage .NET/Java compatible client-side encrypted block blobs.

Please read important points in the Encryption Notes below for more
information.

General Notes
-------------

- blobxfer does not take any leases on blobs or containers. It is up to
  the user to ensure that blobs are not modified while download/uploads
  are being performed.
- No validation is performed regarding container and file naming and length
  restrictions.
- blobxfer will attempt to download from blob storage as-is. If the source
  filename is incompatible with the destination operating system, then
  failure may result.
- When using SAS, the SAS key must be a container-level SAS if performing
  recursive directory upload or container download.
- If uploading via SAS, the container must already be created in blob
  storage prior to upload. This is a limitation of SAS keys. The script
  will force disable container creation if a SAS key is specified.
- For non-SAS requests, timeouts may not be properly honored due to
  limitations of the Azure Python SDK.
- In order to skip download/upload matching files via MD5, the
  computefilemd5 flag must be enabled (it is enabled by default).
- When uploading files as page blobs, the content is page boundary
  byte-aligned. The MD5 for the blob is computed using the final aligned
  data if the source is not page boundary byte-aligned. This enables these
  page blobs or files to be skipped during subsequent download or upload,
  if the skiponmatch parameter is enabled.
- If ``--delete`` is specified, any remote files found that have no
  corresponding local file in directory upload mode will be deleted. Deletion
  occurs prior to any transfers, analogous to the delete-before rsync option.
  Please note that this parameter will interact with ``--include`` and any
  file not included from the include pattern will be deleted.
- ``--include`` has no effect when specifying a single file to upload or
  blob to download. When specifying ``--include`` on container download,
  the pattern will be applied to the blob name without the container name.
  Globbing of wildcards must be disabled such that the script can read
  the include pattern without the shell expanding the wildcards, if specified.
- Due to an underlying issue with the Azure Python Storage SDK, file or blob
  names with ``?`` character in them cannot be uploaded or downloaded using
  a shared storage account key. Please connect with a SAS key if this
  functionality is required.

Performance Notes
-----------------

- Most likely, you will need to tweak the ``--numworkers`` argument that best
  suits your environment. The default is the number of CPUs on the running
  machine multiplied by 3. Increasing this number (or even using the default)
  may not provide the optimal balance between concurrency and your network
  conditions. Additionally, this number may not work properly if you are
  attempting to run multiple blobxfer sessions in parallel from one machine or
  IP address. Futhermore, this number may be defaulted to be set too high if
  encryption is enabled and the machine cannot handle processing multiple
  threads in parallel.
- Using SAS keys may provide the best performance as the script bypasses
  the Azure Storage Python SDK and uses requests/urllib3 directly with
  Azure Storage endpoints.
- As of requests 2.6.0 and Python versions < 2.7.9 (i.e., interpreter found
  on default Ubuntu 14.04 installations), if certain packages are installed,
  as those found in ``requests[security]`` then the underlying ``urllib3``
  package will utilize the ``ndg-httpsclient`` package which will use
  `pyOpenSSL`_. This will ensure the peers are `fully validated`_. However,
  this incurs a rather larger performance penalty. If you understand the
  potential security risks for disabling this behavior due to high performance
  requirements, you can either remove ``ndg-httpsclient`` or use the script
  in a ``virtualenv`` environment without the ``ndg-httpsclient`` package.
  Python versions >= 2.7.9 are not affected by this issue. These warnings can
  be suppressed using ``--disable-urllib-warnings``, but is not recommended
  unless you understand the security implications.

.. _pyOpenSSL: https://urllib3.readthedocs.org/en/latest/security.html#pyopenssl
.. _fully validated: https://urllib3.readthedocs.org/en/latest/security.html#insecureplatformwarning


Encryption Notes
----------------

- **ENCRYPTION SUPPORT IS CONSIDERED RELEASE CANDIDATE QUALITY. ALTHOUGH WE
  CONSIDER ENCRYPTION SUPPORT TO BE USABLE, THERE MAY BE A LATE BREAKING CHANGE
  APPLIED TO BLOBXFER RENDERING ENCRYPTED DATA WITH PRIOR VERSIONS OF BLOBXFER
  UNRECOVERABLE WITH THE CURRENT VERSION. PLEASE WEIGH THIS POSSIBILITY WHEN
  USING BLOBXFER WITH ENCRYPTION FOR PRODUCTION DATA. WE PLAN ON REMOVING RC
  STATUS FROM ENCRYPTION SUPPORT WHEN THE SCRIPT IS CONSIDERED STABLE.**
- Keys for AES256 block cipher are generated on a per-blob basis. These keys
  are encrypted using RSAES-OAEP.
- All required information regarding the encryption process is stored on
  each blob's ``encryptiondata`` and ``encryptiondata_authentication``
  metadata. These metadata entries are used on download to configure the proper
  download and parameters for the decryption process as well as to authenticate
  the encryption. Encryption metadata set by blobxfer (or the Azure Storage
  .NET/Java client library) should not be modified or blobs may be
  unrecoverable.
- MD5 for both the pre-encrypted and encrypted version of the file is stored
  in blob metadata. Rsync-like synchronization is still supported transparently
  with encrypted blobs.
- Whole file MD5 checks are skipped if a message authentication code is found
  to validate the integrity of the encrypted data.
- Attempting to upload the same file as an encrypted blob with a different RSA
  key or under a different encryption mode will not occur if the file content
  MD5 is the same. This behavior can be overridden by including the option
  ``--no-skiponmatch``.
- If one wishes to apply encryption to a blob already uploaded to Storage
  that has not changed, the upload will not occur since the underlying file
  content MD5 has not changed; this behavior can be overriden by including
  the option ``--no-skiponmatch``.
- Encryption is only applied to block blobs. Encrypted page blobs appear to
  be of minimal value stored in Azure. Thus, if uploading VHDs while enabling
  encryption in the script, do not enable the option ``--pageblob``.
  ``--autovhd`` will continue to work transparently where vhd files will be
  uploaded as page blobs in unencrypted form while other files will be
  uploaded as encrypted block blobs.
- Downloading encrypted blobs may not fully preallocate each file due to
  padding. Script failure can result during transfer if there is insufficient
  disk space.
- Zero-byte (empty) files are not encrypted.

Change Log
----------

- 0.10.0: update script for compatibility with azure-storage 0.30.0 which
  is now a required dependency, update cryptography requirement to 1.3,
  promote encryption to RC status, ``--blobep`` now refers to endpoint suffix
  rather than blob endpoint (e.g., core.windows.net rather than
  blob.core.windows.net), added ``--disable-urllib-warnings`` option to
  suppress urllib3 warnings (use with care)
- 0.9.9.11: minor bug fixes, update cryptography requirement to 1.2.2, pin
  azure dependencies due to breaking changes
- 0.9.9.10: fix regression in blob name encoding with Python3
- 0.9.9.9: fix regression in single file upload and remoteresource renaming,
  emit warning when attempting to use remoteresource with a directory upload,
  replace socket exception handling with requests ConnectionError handling,
  properly handle blob names containing ``?`` if using SAS, update setup.py
  dependencies to latest available versions
- 0.9.9.8: disable unnecessary thread daemonization, gracefully handle
  KeyboardInterrupts, explicitly add azure-common to setup.py install reqs
- 0.9.9.7: make base requirements non-optional in import process, update
  azure_request exception handling to support new Azure Storage Python SDK
  errors, reduce number of default concurrent workers to 3x CPU count, change
  azure_request backoff mechanism, add python environment and package info to
  parameter dump to aid issue/bug reports
- 0.9.9.6: add encryption support, fix shared key upload with non-existent
  container, add file overwrite on download option, add auto-detection of file
  mimetype, add remote delete option, fix zero-byte blob download issue,
  replace keeprootdir with strip-components option, add include option,
  reduce the number of default concurrent workers to 4x CPU count
- 0.9.9.5: add file collation support, fix page alignment bug, reduce memory
  usage
- 0.9.9.4: improve page blob upload algorithm to skip empty max size pages.
  fix zero length file uploads. fix single file upload that's skipped.
- 0.9.9.3: fix downloading of blobs with content length of zero
- 0.9.9.1: fix content length > 32bit for blob lists via SAS on Python2
- 0.9.9.0: update script for compatibility with new Azure Python packages
- 0.9.8: fix blob endpoint for non-SAS input, add retry on ServerBusy
- 0.9.7: normalize SAS keys (accept keys with or without ? char prefix)
- 0.9.6: revert local resource path expansion, PEP8 fixes
- 0.9.5: fix directory creation issue
- 0.9.4: fix Python3 compatibility issues
- 0.9.3: the script supports page blob uploading. To specify local files to
  upload as page blobs, specify the ``--pageblob`` parameter. The script also
  has a feature to detect files ending in the ``.vhd`` extension and will
  automatically upload just these files as page blobs while uploading other
  files as block blobs. Specify the ``--autovhd`` parameter (without the
  ``--pageblob`` parameter) to enable this behavior.
- 0.9.0: the script will automatically default to skipping files where if the
  MD5 checksum of either the local file or the stored MD5 of the remote
  resource respectively matches the remote resource or local file, then the
  upload or download for the file will be skipped. This capability will allow
  one to perform rsync-like operations where only files that have changed will
  be transferred. This behavior can be forcefully disabled by specifying
  ``--no-skiponmatch``.
- 0.8.2: performance regression fixes
