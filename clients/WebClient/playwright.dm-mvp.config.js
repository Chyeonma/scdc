import { defineConfig } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
export default defineConfig({testDir:'./e2e/dm-mvp',fullyParallel:false,workers:1,retries:0,timeout:240000,
 outputDir:path.join(root,'.dm-acceptance/e2e-mvp/artifacts'),
 reporter:[['line'],['json',{outputFile:path.join(root,'.dm-acceptance/e2e-mvp/results.json')}]],
 use:{baseURL:'http://localhost:15300',browserName:'chromium',channel:'msedge',headless:true,viewport:{width:1440,height:1000},trace:'off',video:'off',screenshot:'off'}});
