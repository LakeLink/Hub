export default function SignIn() {
    return <div className="mt-12 pb-12 w-full lg:w-5/12 px-4 mx-auto flex flex-col items-center rounded-xl shadow-xl bg-slate-200 text-slate-700">
        <div className="my-6">
            <h2 className="text-center text-3xl font-bold tracking-tight">LakeHub Authentication Center</h2>
            <p className="mt-2 text-center text-sm">
                Connect with your account at 统一身份认证平台/University Portal/SSO
            </p>
        </div>
        <div className="text-red-500"></div>
        <form action="/api/cas/signIn" className="mt-2 space-y-6" method="POST" autoComplete="on">
            <div className="shadow-lg">
                <div>
                    <input type="username" id="username" name="username" required
                        className="block w-full rounded-t-md border border-gray-300 px-3 py-2 text-gray-600 placeholder-gray-500"
                        placeholder="ID" />
                </div>
                <div>
                    <input type="password" id="password" name="password" required
                        className="block w-full rounded-b-md border border-gray-300 px-3 py-2 text-gray-600 placeholder-gray-500"
                        placeholder="Password" />
                </div>
            </div>

            <div className="flex items-center justify-between space-x-4">
                <div className="flex items-center">
                    <input type="checkbox"
                        className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
                    <label className="ml-2 block text-sm">Trust this device</label>
                    <a className="ml-1 text-sm text-indigo-500 hover:text-indigo-600">What?</a>
                </div>

                <div className="text-sm">
                    <a href="https://ssokey.westlake.edu.cn/whistle/retrieve/account"
                        className="font-medium text-indigo-500 hover:text-indigo-600">Forgot your password?</a>
                </div>
            </div>

            <div>
                <button type="submit"
                    className="group relative flex w-full justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                    <span className="absolute inset-y-0 left-0 flex items-center pl-3">

                        {/* <svg className="h-5 w-5 text-indigo-300 group-hover:text-indigo-300"
                            xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                            <path fill-rule="evenodd"
                                d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6z"
                                clip-rule="evenodd" />
                        </svg> */}
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