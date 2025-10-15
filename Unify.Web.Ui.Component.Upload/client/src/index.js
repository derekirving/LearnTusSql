import * as tus from 'tus-js-client'
import packageJson from '../package.json'
import './style.scss'

'use strict';

const uploadsInit = () => {
    console.log(`Unify.Web.Ui.Component.Upload-> Version: ${packageJson.version}`);
    console.log("Unify.Web.Ui.Component.Upload-> Tus is supported and can store URLs", tus.isSupported, tus.canStoreURLs);
}

;(function (e, t, n) {
    const r = e.querySelectorAll("body")[0];
    r.className = r.className.replace(/(^|\s)no-js(\s|$)/, "$1js$2");
    window.unify = window.unify || {};
    window.unify.uploadsInit = uploadsInit;
    window.unify.uploadsInit();
})(document, window, 0);