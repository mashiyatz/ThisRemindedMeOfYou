/* @refresh reload */
import { render } from 'solid-js/web';
import { App } from './App';
import { initUnityBridge } from './bridge/UnityBridge';

initUnityBridge();
render(() => <App />, document.getElementById('root')!);
