// import * as tus from 'tus-js-client'
// import packageJson from '../package.json'
// import './style.scss';

'use strict';
const uploadsInit = () => {
        const isAdvancedUpload = function () {
            var div = document.createElement('div');
            return (('draggable' in div) || ('ondragstart' in div && 'ondrop' in div)) && 'FormData' in window && 'FileReader' in window;
        }();

        console.log(`Unify.Web.Ui.Component.Upload-> Version: dev`);
        console.log("Unify.Web.Ui.Component.Upload-> Tus Supported", tus.isSupported);
        
        document.querySelectorAll('.zone__remove-file').forEach((element) => {
            element.addEventListener('click', (e) => {
                const fileName = element.dataset.fileName;
                
                if(!window.htmx) {
                    e.preventDefault();
                    if (confirm(`Really delete ${fileName}?`)) {
                        window.location.href = element.href;
                    }
                }
            })
        });

    if (window.htmx && !window.unify._htmxConfirmRegistered) {
        document.body.addEventListener('htmx:confirm', function (event) {
            if (event.target.classList.contains('zone__remove-file') && isHtmxElem(event)) {
                event.preventDefault();
                const fileName = event.target.dataset.fileName;
                if (confirm(`Really delete ${fileName}?`)) {
                    event.detail.issueRequest();
                }
            }
        });
        window.unify._htmxConfirmRegistered = true;
    }
        
        if (window.htmx) {
            console.log("Unify.Web.Ui.Component.Upload-> htmx available at Version", window.htmx.version)
            
            document.body.addEventListener('htmx:afterSwap', function (evt) {
                console.log('Unify.Web.Ui.Component.Upload-> htmx:afterSwap re-initialisation');
                if (evt.target.querySelector('.zone__file')) {
                    window.unify.uploadsInit();
                }
            });
        };

        const isHtmxElem = (event) => {
            return event.target.hasAttribute('hx-post') ||
                event.target.hasAttribute('hx-get') ||
                event.target.hasAttribute('hx-put') ||
                event.target.hasAttribute('hx-delete')
        };

        const onSuccess = function (payload, form, zone, submitBtn, file, url, event) {

            const { lastResponse } = payload;
            const contentLocation = lastResponse.getHeader('Content-Location');
            
            console.log('contentLocation', contentLocation);
            
            zone.dataset.fileCount--;

            zone.classList.add('is-success');
            zone.classList.remove('is-uploading');
            zone.classList.remove('is-error');

            const fileInput = form.querySelector('input[type="file"]');
            if (fileInput) fileInput.value = '';

            const fileId = url.split("/").pop();

            const hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = 'fileId';
            hidden.value = fileId;
            form.appendChild(hidden);

            document.dispatchEvent(new CustomEvent('unify-upload-success', {
                detail: {form, zone: zone.dataset.zone, fileId, url, contentLocation, event},
            }));

            if (parseInt(zone.dataset.fileCount, 10) === 0) {
                performSubmit(form, submitBtn, true, event);
            }
        }

        const performSubmit = function (form, submitBtn, hasFiles, event) {
            document.dispatchEvent(new CustomEvent('unify-upload-form-submitting', {
                detail: {form: form, submitButton: submitBtn, hasFiles: hasFiles, event: event},
            }));

            if (form.classList.contains('submit-after-unify-uploads')) {

                if (!event) {
                    form.submit();
                } else {
                    event.detail.issueRequest();
                }
            }
        }

        const performUpload = function (form, submitBtn, zone, files, event) {

            zone.classList.add('is-uploading');
            const zoneId = zone.dataset.zone;
            const progressBar = zone.querySelector('.progress-bar');

            Array.prototype.forEach.call(files, function (file) {
                const upload = new tus.Upload(file,
                    {
                        endpoint: 'unify/uploads/',
                        retryDelays: [],
                        onSuccess: (payload) => onSuccess(payload, form, zone, submitBtn, file, upload.url, event),
                        onProgress: (bytesUploaded, bytesTotal) => {
                            const percentage = ((bytesUploaded / bytesTotal) * 100).toFixed(2);
                            progressBar.style.width = percentage + '%';
                            progressBar.textContent = `${percentage}% - ${file.name}`;
                        },
                        metadata: {
                            name: file.name,
                            contentType: file.type || 'application/octet-stream',
                            size: file.size,
                            zone: zoneId
                        },
                        onError: (err) => {
                            // console.log("Error", err)
                            // console.log("Request", err.originalRequest)
                            // console.log("Response", err.originalResponse)
                            // console.log("Error", err.originalResponse._xhr.responseText)

                            zone.classList.remove('is-uploading');
                            submitBtn.disabled = false;
                            console.log(err);
                            alert(err.originalResponse._xhr.responseText)
                        },
                        onShouldRetry: (err, retryAttempt, options) => {
                            // console.log("Error", err)
                            // console.log("Request", err.originalRequest)
                            // console.log("Response", err.originalResponse)

                            var status = err.originalResponse ? err.originalResponse.getStatus() : 0
                            if (status === 401 || status === 403) {
                                return false
                            }

                            // For any other status code, we retry.
                            return true
                        }
                    });

                upload.findPreviousUploads().then(function (previousUploads) {

                    if (previousUploads.length) {
                        upload.resumeFromPreviousUpload(previousUploads[0]);
                    }
                    upload.start();

                }).catch(function () {
                    upload.start();
                });
            });
        }

        var zones = document.querySelectorAll('.zone');
        Array.prototype.forEach.call(zones, function (zone) {

            var zoneId = zone.dataset.zone;
            var maxFiles = parseInt(zone.dataset.maxFiles);
            var minFiles = parseInt(zone.dataset.minFiles);
            var maxFileSize = parseInt(zone.dataset.maxFileSize);
            var acceptedFiles = zone.dataset.accepted.split(',').map(ext => ext.trim().toLowerCase());

            var form = zone.closest('form');
            var input = zone.querySelector('input[type="file"]'),
                label = zone.querySelector('label'),
                errorMsg = zone.querySelector('.zone__error span'),
                restart = zone.querySelectorAll('.zone__restart'),
                droppedFiles = false,
                selectedFiles = [],
                submitBtn = form.querySelector('[type="submit"]'),
                showFiles = function (files) {

                    Array.from(files).forEach(file => {

                        if (file.size > maxFileSize) {
                            console.warn("File too large", file.size, maxFileSize)
                            alert(`File too large ${file.name}`);
                            return;
                        }

                        const fileExt = file.name.split('.').pop().toLowerCase();
                        if (acceptedFiles.includes(fileExt)) {
                            if (!selectedFiles.some(f => f.name === file.name && f.size === file.size)) {
                                selectedFiles.push(file);
                            }
                        } else {
                            alert("Incorrect file extension. You can only upload: " + acceptedFiles.map(ext => '.' + ext).join(', '));
                        }
                    });

                    var label = document.querySelector(`label[for='file_${zoneId}']`);

                    if (selectedFiles.length === maxFiles) {
                        input.disabled = true;
                        label.style.cursor = 'not-allowed';
                        zone.style.cursor = 'no-drop';
                    } else {
                        input.disabled = false;
                        label.style.cursor = 'pointer';
                        zone.style.cursor = 'default';
                    }

                    const fileList = zone.querySelector('.fileList');
                    if (!fileList) return;

                    fileList.innerHTML = '';
                    const ul = document.createElement('ul');
                    selectedFiles.forEach((file, idx) => {
                        const li = document.createElement('li');
                        li.innerHTML = `<span><svg class="mb-1 me-1" width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M4 2H14L20 8V20C20 21.1 19.1 22 18 22H6C4.9 22 4 21.1 4 20V2Z" stroke="gray" stroke-width="2" stroke-linejoin="round"/>
  <path d="M14 2V8H20" stroke="gray" stroke-width="2" stroke-linejoin="round"/>
</svg>${file.name} <small class="text-muted">(${(file.size / 1024).toFixed(1)} KB)</small></span>`;

                        const removeBtn = document.createElement('a');
                        removeBtn.href = file.name;
                        removeBtn.innerHTML = `<svg width="16" height="16" viewBox="0 0 16 16" fill="none"
    xmlns="http://www.w3.org/2000/svg">
  <line x1="4" y1="4" x2="12" y2="12" stroke="red" stroke-width="2"/>
  <line x1="12" y1="4" x2="4" y2="12" stroke="red" stroke-width="2"/>
</svg>`;
                        removeBtn.setAttribute('aria-label', 'Remove file');
                        removeBtn.addEventListener('click', (e) => {
                            e.preventDefault();
                            selectedFiles.splice(idx, 1);
                            showFiles([]);
                        });

                        li.appendChild(removeBtn);
                        ul.appendChild(li);
                    });

                    fileList.appendChild(ul);

                    zone.dataset.fileCount = selectedFiles.length;

                    const totalSize = selectedFiles.reduce((sum, file) => sum + file.size, 0);
                    const totalSizeKB = (totalSize / 1024).toFixed(1);

                    label.innerHTML = selectedFiles.length === 1
                        ? `1 file selected (${totalSizeKB} KB)`
                        : `${selectedFiles.length} files selected (${totalSizeKB} KB)`;

                    if (maxFiles > 1 && selectedFiles.length < maxFiles) {
                        label.innerHTML += " <span><strong>Choose more</strong></span>"
                    }

                    document.dispatchEvent(new CustomEvent('unify-upload-files-changed', {
                        detail: {zone: zone.dataset.zone, count: selectedFiles.length},
                    }));
                },
                triggerFormSubmit = function (event) {

                    if (minFiles > 0 && parseInt(zone.dataset.fileCount, 10) < minFiles) {
                        const textElem = document.querySelector(`[data-for="${zone.dataset.zone}"]`);
                        const text = textElem ? textElem.textContent : "zone";
                        const plural = minFiles === 1 ? "file" : "files";
                        alert(`A minimum of ${minFiles} ${plural} must be added to "${text}"`);
                        return;
                    }

                    if (submitBtn) {
                        submitBtn.disabled = true;
                    }
                    const files = selectedFiles.length ? selectedFiles : input.files;
                    selectedFiles = [];

                    if (files.length === 0) {
                        performSubmit(form, submitBtn, false, event);
                    } else {
                        performUpload(form, submitBtn, zone, files, event);
                    }
                };

            if (submitBtn) {
                if (window.htmx) {
                    document.body.addEventListener('htmx:confirm', function (event) {

                        console.log("htmx:confirm");

                        if (event.target.classList.contains('submit-after-unify-uploads') && isHtmxElem(event)) {
                            // Cancel the automatic request
                            console.log("htmx:confirm doing things before submit", event)
                            triggerFormSubmit(event);
                            event.preventDefault();
                            //event.detail.issueRequest();
                        }
                    }, {once: true});
                } else {
                    submitBtn.addEventListener('click', (e) => {
                        console.log(parseInt(zone.dataset.fileCount, 10), minFiles)
                        e.preventDefault();
                        triggerFormSubmit(false);
                    });
                }
            }

            input.addEventListener('change', function (e) {
                showFiles(e.target.files);
                // automatically submit the form on file select
                //triggerFormSubmit();
            });

            if (isAdvancedUpload) {
                zone.classList.add('has-advanced-upload');

                ['drag', 'dragstart', 'dragend', 'dragover', 'dragenter', 'dragleave', 'drop'].forEach(function (event) {
                    zone.addEventListener(event, function (e) {
                        e.preventDefault();
                        e.stopPropagation();
                    });
                });
                ['dragover', 'dragenter'].forEach(function (event) {
                    zone.addEventListener(event, function () {

                        if (selectedFiles.length === maxFiles) {
                            return;
                        }

                        zone.style.cursor = 'default';
                        zone.classList.add('is-dragover');
                    });
                });
                ['dragleave', 'dragend', 'drop'].forEach(function (event) {
                    zone.addEventListener(event, function () {
                        zone.classList.remove('is-dragover');
                    });
                });
                zone.addEventListener('drop', function (e) {

                    const dropped = Array.from(e.dataTransfer.files);
                    if (selectedFiles.length + dropped.length > maxFiles) {
                        return;
                    }

                    droppedFiles = e.dataTransfer.files; // the files that were dropped
                    showFiles(droppedFiles);
                    //triggerFormSubmit();
                });
            }
        });
    }

;(function (e, t, n) {
    var r = e.querySelectorAll("html")[0];
    r.className = r.className.replace(/(^|\s)no-js(\s|$)/, "$1js$2");
    window.unify = window.unify || {};
    window.unify.uploadsInit = uploadsInit;
    window.unify.uploadsInit();
})(document, window, 0);

