import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const source = await readFile(new URL('../../Src/CurrencyCalculator.Web/wwwroot/js/browser-storage.js', import.meta.url), 'utf8');

test('存储被禁用或已满时返回可用默认值，只通知一次', () => {
    let notices = 0;
    const context = {
        Event,
        window: { dispatchEvent() { notices++; } },
        localStorage: {
            getItem() { throw new Error('blocked'); },
            setItem() { throw new Error('quota'); },
            removeItem() { throw new Error('blocked'); }
        }
    };
    vm.runInNewContext(source, context);
    const storage = context.window.ccStorage;
    assert.equal(storage.get('theme'), null);
    assert.equal(storage.set('rates', '{}'), false);
    storage.remove('rates');
    assert.equal(storage.available, false);
    assert.equal(notices, 1);
});
