import { LockClosedIcon } from "@heroicons/react/24/solid"
import { browserSupportsWebAuthn, startAuthentication } from "@simplewebauthn/browser";
import { useState, useEffect, useRef, FormEvent } from "react";

export default function SignIn() {
  const [webAuthnAvailable, setWebAuthnAvailable] = useState(false)
  const [pwdFieldHidden, setPwdFieldHidden] = useState(true)
  const casId = useRef('')

  useEffect(() => {
    const checkAvailability = async () => {
      const available =
        browserSupportsWebAuthn();
      setWebAuthnAvailable(available);
    };
    checkAvailability();
  }, []);

  async function onSubmit(e: FormEvent) {
    if (!pwdFieldHidden) return
    e.preventDefault()

    let r = await fetch('/api/auth/webAuthn/genAuthOptions', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        casId: casId.current
      })
    })

    if (!r.ok) {
      setPwdFieldHidden(false)
      return
    }

    try {
      const asseResp = await startAuthentication(await r.json())
      console.log(asseResp)
      const verification = await fetch('/api/auth/webAuthn/auth', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(asseResp)
      })
      console.log(await verification.json())
    } catch (error) {
      throw error;
    }
  }

  return <div className="my-12 pb-12 w-full lg:w-5/12 px-4 mx-auto flex flex-col items-center rounded-xl shadow-xl bg-slate-200 text-slate-700">
    <div className="my-6">
      <h2 className="text-center text-3xl font-bold tracking-tight">LakeHub Authentication Center</h2>
      <p className="mt-2 text-center text-sm">
        Connect with your account at 统一身份认证平台/University Portal/SSO
      </p>
    </div>
    <div className="text-red-500"></div>
    <form action="/api/cas/signIn" className="mt-2 space-y-6" method="POST" autoComplete="on" onSubmit={onSubmit}>
      <div className="shadow-lg">
        <div>
          <input type="username" id="username" name="username" onChange={(event) => casId.current = event.target.value} autoComplete="webauthn username" required
            className="block w-full rounded-t-md border border-gray-300 px-3 py-2 text-gray-600 placeholder-gray-500"
            placeholder="ID" />
        </div>
        <div>
          {!pwdFieldHidden && <input type="password" id="password" name="password"
            className="block w-full rounded-b-md border border-gray-300 px-3 py-2 text-gray-600 placeholder-gray-500"
            placeholder="Password" />
          }
        </div>
      </div>

      <div className="flex items-center justify-between space-x-4">
        <div className="flex items-center">
          <input type="checkbox"
            className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
          <label className="ml-2 block text-sm">Trust this device</label>
          <a className="ml-1 text-sm text-indigo-500 hover:text-indigo-600">What?</a>
        </div>

        <div className="text-sm" hidden={pwdFieldHidden}>
          <a href="https://ssokey.westlake.edu.cn/whistle/retrieve/account"
            className="font-medium text-indigo-500 hover:text-indigo-600">Forgot your password?</a>
        </div>
      </div>

      <div>
        <button type="submit"
          className="group relative flex w-full justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
          <span className="absolute inset-y-0 left-0 flex items-center pl-3">
            <LockClosedIcon className="h-5 w-5 text-indigo-300 group-hover:text-indigo-300"></LockClosedIcon>
          </span>
          Sign in
        </button>
      </div>
    </form>

    <p className="mt-2 text-sm text-slate-500">
      By clicking [Sign in], I understand and agree to the <a className="text-indigo-500 hover:text-indigo-600">Privacy Policy</a>
    </p>
  </div>
}