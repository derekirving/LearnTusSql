import * as tus from 'tus-js-client'
import packageJson from '../package.json'
import './style.scss'

'use strict';

const uploadsInit = () => {

    const baseUrl = document.querySelector('meta[name="unify-upload-baseUrl"]')?.getAttribute('content');
    if (!baseUrl) {
        throw new Error('No \"unify-upload-baseUrl\" meta tag found on html');
    }

    const APP_ID = document.querySelector('meta[name="unify-upload-id"]')?.getAttribute('content');
    if (!APP_ID) {
        throw new Error('No \"unify-upload-id\" meta tag found on html');
    }

    const ENDPOINT = `${baseUrl}/unify/uploads/`;

    console.log(`Unify.Web.Ui.Component.Upload-> Version: ${packageJson.version}`);
    console.log("Unify.Web.Ui.Component.Upload-> Tus is supported and can store URLs", tus.isSupported, tus.canStoreURLs);
    console.log(`Unify.Web.Ui.Component.Upload-> Endpoint: ${ENDPOINT}`);

    const SVG_DOC = '<svg class="mb-1 me-1" width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">\n' +
        '  <path d="M4 2H14L20 8V20C20 21.1 19.1 22 18 22H6C4.9 22 4 21.1 4 20V2Z" stroke="gray" stroke-width="2" stroke-linejoin="round"/>\n' +
        '  <path d="M14 2V8H20" stroke="gray" stroke-width="2" stroke-linejoin="round"/>\n' +
        '</svg>';

    const SVG_REMOVE = '<svg width="16" height="16" viewBox="0 0 16 16" fill="none"\n' +
        '    xmlns="http://www.w3.org/2000/svg">\n' +
        '  <line x1="4" y1="4" x2="12" y2="12" stroke="red" stroke-width="2"/>\n' +
        '  <line x1="12" y1="4" x2="4" y2="12" stroke="red" stroke-width="2"/>\n' +
        '</svg>'

    const isAdvancedUpload = (() => {
        const div = document.createElement('div');
        return (('draggable' in div) || ('ondragstart' in div && 'ondrop' in div)) && 'FormData' in window && 'FileReader' in window;
    })();

    const isHtmxElem = (event) => {
        return event.target.hasAttribute('hx-post') ||
            event.target.hasAttribute('hx-get') ||
            event.target.hasAttribute('hx-put') ||
            event.target.hasAttribute('hx-delete')
    };

    if (window.htmx) {
        console.log("Unify.Web.Ui.Component.Upload-> htmx available at Version", window.htmx.version)

        document.body.addEventListener('htmx:afterSwap', function (evt) {
            console.log('Unify.Web.Ui.Component.Upload-> htmx:afterSwap re-initialisation');
            if (evt.target.querySelector('.zone__file')) {
                window.unify.uploadsInit();
            }
        });
    }

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

    document.querySelectorAll('.zone__remove-file').forEach((element) => {
        element.addEventListener('click', (e) => {
            const fileName = element.dataset.fileName;

            if (!window.htmx) {
                e.preventDefault();
                if (confirm(`Really delete ${fileName}?`)) {
                    window.location.href = element.href;
                }
            }
        })
    });

    const enableHtmx = (submitBtn, zone, minFiles, triggerFormSubmit) => {
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
                    //console.log(parseInt(zone.dataset.fileCount, 10), minFiles)
                    e.preventDefault();
                    triggerFormSubmit(false);
                });
            }
        }
    };
    
    const appendHidden = (form, name, value) => {
        const hidden = document.createElement('input');
        hidden.type = 'hidden';
        hidden.name = name;
        hidden.value = value;
        form.appendChild(hidden);
    }

    const onSuccess = (payload, form, zone, submitBtn, file, url, event, count) => {
        const {lastResponse} = payload;
        const contentLocation = lastResponse.getHeader('Content-Location');

        console.log('onSuccess', {payload, form, zone, submitBtn, file, url, event, count})
        console.log('contentLocation', contentLocation);
        
        const dto  = zone.dataset.dto;

        zone.dataset.fileCount--;

        if (form.classList.contains('submit-after-unify-uploads')) {
            zone.classList.add('is-success');
        }else{
            submitBtn.disabled = false;
        }
        
        zone.classList.remove('is-uploading');
        zone.classList.remove('is-error');

        const fileInput = form.querySelector('input[type="file"]');
        if (fileInput) fileInput.value = '';

        const fileId = url.split("/").pop();
        
        appendHidden(form, `${dto}[${count}].FileId`, fileId);
        appendHidden(form, `${dto}[${count}].FileName`, file.name);
        appendHidden(form, `${dto}[${count}].Size`, file.size);
        appendHidden(form, `${dto}[${count}].Uri`, url);
        appendHidden(form, `${dto}[${count}].Zone`, zone.dataset.zone);

        document.dispatchEvent(new CustomEvent('unify-upload-success', {
            detail: {form, zone: zone.dataset.zone, fileId, url, contentLocation, event},
        }));

        if (parseInt(zone.dataset.fileCount, 10) === 0) {
            performSubmit(form, submitBtn, true, event);
        }
    }

    const performUpload = (form, submitBtn, zone, files, event) => {
        console.log("performUpload", form, submitBtn, zone, files, event);

        const uploadIdElem = form.querySelector('.unify-upload-id');
        if(!uploadIdElem){
            throw new Error("Unable to find unify upload id on form");
        }

        const uploadId = uploadIdElem.value || uploadIdElem.getAttribute('value');

        zone.classList.add('is-uploading');
        
        const zoneId = zone.dataset.zone;
        const progressBar = zone.querySelector('.progress-bar');

        for (const [index, file] of files.entries()) {
            const upload = new tus.Upload(file,
                {
                    endpoint: ENDPOINT,
                    retryDelays: [],
                    onSuccess: (payload) => onSuccess(payload, form, zone, submitBtn, file, upload.url, event, index),
                    onProgress: (bytesUploaded, bytesTotal) => {
                        const percentage = ((bytesUploaded / bytesTotal) * 100).toFixed(2);
                        progressBar.style.width = percentage + '%';
                        progressBar.textContent = `${percentage}% - ${file.name}`;
                    },
                    metadata: {
                        name: file.name,
                        contentType: file.type || 'application/octet-stream',
                        size: file.size,
                        zoneId: zoneId,
                        uploadId: uploadId,
                        appId: APP_ID
                    },
                    onError: (err) => {
                        zone.classList.remove('is-uploading');
                        submitBtn.disabled = false;

                        document.dispatchEvent(new CustomEvent('unify-upload-filed', {
                            detail: {form, zone: zone.dataset.zone, event},
                        }));

                        const url = err.originalRequest._url;
                        if (err.originalResponse && err.originalResponse._xhr) {
                            const status = err.originalResponse._xhr.status;
                            const statusText = err.originalResponse._xhr.statusText;
                            const responseText = err.originalResponse._xhr.responseText || 'No more information available.';

                            alert(`Uploading to ${url} failed.\nStatus: ${status}\nStatusText: ${statusText}\nResponse: ${responseText}`);
                        } else {
                            console.log(err);
                        }
                    },
                    onShouldRetry: (err, retryAttempt, options) => {
                        // console.log("Error", err)
                        // console.log("Request", err.originalRequest)
                        // console.log("Response", err.originalResponse)

                        const status = err.originalResponse ? err.originalResponse.getStatus() : 0
                        if (status === 401
                            || status === 403
                            || status === 404) {
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
        }
    }

    const performSubmit = (form, submitBtn, hasFiles, event) => {
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

    const zones = document.querySelectorAll('.zone');

    for (const zone of zones) {

        const zoneId = zone.dataset.zone;
        const acceptedFiles = zone.dataset.accepted.split(',').map(ext => ext.trim().toLowerCase());
        const minFiles = parseInt(zone.dataset.minFiles);
        const maxFiles = parseInt(zone.dataset.maxFiles);
        const maxFileSize = parseInt(zone.dataset.maxFileSize);

        const form = zone.closest('form');
        const input = zone.querySelector('input[type="file"]');
        const label = document.querySelector(`label[for='file_${zoneId}']`);
        const fileList = zone.querySelector('.fileList');

        let submitBtn = form?.querySelector('[type="submit"]');

        let selectedFiles = [];
        const triggerFormSubmit = (event) => {
            console.log("submit", form);

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
        }
        const updateUI = () => {
            input.disabled = selectedFiles.length === maxFiles;
            label.style.cursor = input.disabled ? 'not-allowed' : 'pointer';
            zone.style.cursor = input.disabled ? 'no-drop' : 'default';

            if (!fileList) return;
            fileList.innerHTML = '';
            const ul = document.createElement('ul');
            selectedFiles.forEach((file, idx) => {
                const li = document.createElement('li');
                li.innerHTML = `<span>${SVG_DOC}${file.name} <small class="text-muted">(${(file.size / 1024).toFixed(1)} KB)</small></span>`;
                const removeBtn = document.createElement('a');
                removeBtn.href = '#';
                removeBtn.setAttribute('aria-label', 'Remove file');
                removeBtn.innerHTML = SVG_REMOVE;
                removeBtn.addEventListener('click', e => {
                    e.preventDefault();
                    selectedFiles.splice(idx, 1);
                    updateUI();
                });
                li.appendChild(removeBtn);
                ul.appendChild(li);
            });
            fileList.appendChild(ul);

            zone.dataset.fileCount = selectedFiles.length.toString();
            const totalSizeKB = (selectedFiles.reduce((sum, file) => sum + file.size, 0) / 1024).toFixed(1);
            label.innerHTML = `${selectedFiles.length} file${selectedFiles.length === 1 ? '' : 's'} selected (${totalSizeKB} KB)`;
            if (maxFiles > 1 && selectedFiles.length < maxFiles) {
                label.innerHTML += " <span><strong>Choose more</strong></span>";
            }
            document.dispatchEvent(new CustomEvent('unify-upload-files-changed', {
                detail: {zone: zoneId, count: selectedFiles.length},
            }));
        };

        const showFiles = files => {
            for (const file of files) {
                if (file.size > maxFileSize) {
                    alert(`File too large: ${file.name}`);
                    continue;
                }
                const fileExt = file.name.split('.').pop().toLowerCase();
                if (!acceptedFiles.includes(fileExt)) {
                    alert("Incorrect file extension. Allowed: " + acceptedFiles.map(ext => '.' + ext).join(', '));
                    continue;
                }
                if (!selectedFiles.some(f => f.name === file.name && f.size === file.size)) {
                    selectedFiles.push(file);
                }
            }
            updateUI();
        };

        input.addEventListener('change', function (e) {
            showFiles(e.target.files);
            // can automatically submit the form on file select
            //triggerFormSubmit();
        });

        if (isAdvancedUpload) {

            zone.classList.add('has-advanced-upload');
                
            const dragEvents = ['drag', 'dragstart', 'dragend', 'dragover', 'dragenter', 'dragleave', 'drop'];
            const preventDefaultHandler = e => {
                e.preventDefault();
                e.stopPropagation();
            };

            const dragOverHandler = (zone, selectedFiles, maxFiles) => () => {
                if (selectedFiles.length === maxFiles) return;
                zone.style.cursor = 'default';
                zone.classList.add('is-dragover');
            };

            const dragLeaveHandler = zone => () => {
                zone.classList.remove('is-dragover');
            };

            const dropHandler = (zone, selectedFiles, maxFiles, showFiles) => e => {
                const dropped = Array.from(e.dataTransfer.files);
                if (selectedFiles.length + dropped.length > maxFiles) return;
                showFiles(dropped);
            };

            enableHtmx(submitBtn, zone, minFiles, triggerFormSubmit);

            dragEvents.forEach(event => {
                zone.addEventListener(event, preventDefaultHandler);
            });

            ['dragover', 'dragenter'].forEach(event => {
                zone.addEventListener(event, dragOverHandler(zone, selectedFiles, maxFiles));
            });

            ['dragleave', 'dragend', 'drop'].forEach(event => {
                zone.addEventListener(event, dragLeaveHandler(zone));
            });

            zone.addEventListener('drop', dropHandler(zone, selectedFiles, maxFiles, showFiles));
        }
    }
};

(function (e, t, n) {
    const r = e.querySelectorAll("body")[0];
    r.className = r.className.replace(/(^|\s)no-js(\s|$)/, "$1js$2");
    window.unify = window.unify || {};
    window.unify.uploadsInit = uploadsInit;
    window.unify.uploadsInit();
})(document, window, 0);
