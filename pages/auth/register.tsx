import { FormEvent, useEffect, useState } from "react";
import { withIronSessionSsr } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { browserSupportsWebAuthn, startRegistration } from '@simplewebauthn/browser'
import { ObjectId } from "mongodb";
import mongo from "~/lib/mongo";
import { User } from "~/lib/user";
import { InferGetServerSidePropsType } from "next/types";
import { genRegistration } from "~/lib/webAuthn";
import { LockClosedIcon } from "@heroicons/react/24/solid";

export default function Register({ options, realName }: InferGetServerSidePropsType<typeof getServerSideProps>) {
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  useEffect(() => {
    const checkAvailability = async () => {
      const available =
        browserSupportsWebAuthn();
      setIsAvailable(available);
    };

    checkAvailability();
  }, []);

  async function handleClick() {
    let attResp = await startRegistration(options);
    const verificationResp = await fetch("/api/auth/webAuthn/register", {
      method: "POST",
      body: JSON.stringify(attResp),
      headers: {
        "Content-Type": "application/json",
      },
    });

    const result = await verificationResp.json();
    if (result.success) {
      document.location.pathname = '/'
    }
  };

  return (
    <>
      <div className="my-12 pb-12 w-full lg:w-8/12 px-4 mx-auto flex flex-col items-center rounded-xl shadow-xl bg-slate-200 text-slate-700">
        <div className="mt-6">
          <h2 className="text-center text-3xl font-bold tracking-tight">Setup WebAuthn</h2>
          <p className="my-2 text-center text-sm">
            Hello {realName}, welcome to setup the most secure login method, ever
          </p>
        </div>
        {isAvailable ? (
          <button className="group relative flex w-30 justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2" onClick={handleClick}>
            Setup
            <LockClosedIcon className="ml-2 h-5 w-5 text-indigo-300 group-hover:text-indigo-300"></LockClosedIcon>
          </button>
        ) : (
          <p>Sorry, webauthn is not available.</p>
        )}
      </div>
    </>
  );
}

export const getServerSideProps = withIronSessionSsr(async function ({
  req,
  res,
}) {
  const col = (await mongo).db('lakehub').collection('users')

  if (!req.session.userId) return {
    redirect: {
      destination: '/auth/signIn',
      permanent: false
    }
  }

  const user = await col.findOne<User>({ _id: new ObjectId(req.session.userId) });
  const options = genRegistration(user)

  req.session.challenge = options.challenge
  await req.session.save();

  return { props: { options, realName: user.realName } };
},
  sessionOptions);