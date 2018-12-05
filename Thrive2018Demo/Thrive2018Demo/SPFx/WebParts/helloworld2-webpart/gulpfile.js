'use strict';

const gulp = require('gulp');
const build = require('@microsoft/sp-build-web');
build.addSuppression(`Warning - [sass] The local CSS class 'ms-Grid' is not camelCase and will not be type-safe.`);

var args = require('yargs').argv;

var copyToWspTaskPre = build.subTask('copytowsp-pre', (gulp, buildConfig, done) => {
    var copyAssetsConfig = require("./config/copy-assets");
    var clean = require("gulp-clean");

    return gulp.src(copyAssetsConfig.deployCdnPath, {read: false})
    .pipe(clean({force: true}));
});

var modifyConfigFile = build.subTask('modifyconfigfile-pre', (gulp, buildConfig, done) => {
    var configFC = require("./config/config");
    
    try {
        require("./temp/config-local");
    }
    catch (Err) {
        fs.writeFileSync("./temp/config-local.json", JSON.stringify(configFC, null, 2));
    }

    Object.keys(configFC.externals).forEach(element => {
        var extJS = configFC.externals[element];
        if (extJS.customJSPath) {
            extJS.path = args.targetCdn + "/" + extJS.customJSPath;
        }
    });

    fs.writeFileSync("./config/config.json", JSON.stringify(configFC, null, 2));

    return gulp.src("./temp/config-local.json");
});
build.task('modifyconfigfile', modifyConfigFile);

var cleanconfigfile = build.subTask('cleanconfigfile-post', (gulp, buildConfig, done) => {
    var configFC = require("./temp/config-local");
    fs.writeFileSync("./config/config.json", JSON.stringify(configFC, null, 2));

    var clean = require("gulp-clean");
    return gulp.src("./temp/config-local.json").pipe(clean({force: true}));
});
build.task('cleanconfigfile', cleanconfigfile);

var copyAssetsToWspTaskPost = build.subTask('copyassetstowsp-post', (gulp, buildConfig, done) => {
    var copyAssetsConfig = require("./config/copy-assets");
    var copyToWSPConfig = require("./config/copy-to-wsp");

    return gulp.src(copyAssetsConfig.deployCdnPath + "/*")
    .pipe(gulp.dest(copyToWSPConfig.deployWspPath));
});
build.task('copyassetstowsp', copyAssetsToWspTaskPost);

var copyPackageToWspTaskPost = build.subTask('copypackagetowsp-post', (gulp, buildConfig, done) => {
    var copyToWSPConfig = require("./config/copy-to-wsp");
   
    return gulp.src("./sharepoint/solution/*.sppkg")
    .pipe(gulp.dest(copyToWSPConfig.deployWspPath));
});
build.task('copypackagetowsp', copyPackageToWspTaskPost);

if (args.copytowsp) {
    build.copyAssets.taskConfig = { excludeHashFromFileNames: true, }
    build.rig.addPreBuildTask(copyToWspTaskPre);
}

const rewrite = require('spfx-build-url-rewrite');
rewrite.config(build);

build.initialize(gulp);
